#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <dirent.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <ftw.h>
#include "maku_common.h"

static int maku_write_all_cpu_governors(const char *value) {
    long cpu_count = maku_get_online_cpu_count();
    int rc = 0;
    for (long i = 0; i < cpu_count; i++) {
        char path[256];
        snprintf(path, sizeof(path), MAKU_CPU_GOVERNOR_FMT, (int)i);
        if (maku_write_sysfile(path, value) != 0) {
            rc = -1;
        }
    }
    return rc;
}

static const char *maku_service_unit_name(const char *base, MakuInitSystem init) {
    static char buf[128];
    switch (init) {
        case MAKU_INIT_SYSTEMD:
            snprintf(buf, sizeof(buf), "%s.service", base);
            return buf;
        default:
            return base;
    }
}

static int maku_service_control(const char *service, int enable) {
    MakuInitSystem init = maku_detect_init_system();
    const char *unit = maku_service_unit_name(service, init);

    char *argv[8];
    int idx = 0;

    switch (init) {
        case MAKU_INIT_SYSTEMD:
            argv[idx++] = "systemctl";
            argv[idx++] = enable ? "enable" : "disable";
            argv[idx++] = "--now";
            argv[idx++] = (char *)unit;
            argv[idx] = NULL;
            break;
        case MAKU_INIT_OPENRC:
            argv[idx++] = "rc-update";
            argv[idx++] = enable ? "add" : "del";
            argv[idx++] = (char *)service;
            argv[idx++] = "default";
            argv[idx] = NULL;
            break;
        case MAKU_INIT_RUNIT: {
            char path[256];
            snprintf(path, sizeof(path), "/var/service/%s", service);
            if (enable) {
                char source[256];
                snprintf(source, sizeof(source), "/etc/sv/%s", service);
                symlink(source, path);
            } else {
                unlink(path);
            }
            return 0;
        }
        default:
            return -1;
    }

    pid_t pid = fork();
    if (pid < 0) return -1;
    if (pid == 0) {
        execvp(argv[0], argv);
        _exit(127);
    }
    int status = 0;
    waitpid(pid, &status, 0);
    return WIFEXITED(status) ? WEXITSTATUS(status) : -1;
}

static int maku_rmrf_callback(const char *fpath, const struct stat *sb, int typeflag, struct FTW *ftwbuf) {
    (void)sb;
    (void)ftwbuf;
    if (typeflag == FTW_DP || typeflag == FTW_D) {
        rmdir(fpath);
    } else {
        unlink(fpath);
    }
    return 0;
}

static void maku_rmrf_contents(const char *dirpath) {
    DIR *d = opendir(dirpath);
    if (!d) return;
    struct dirent *entry;
    while ((entry = readdir(d)) != NULL) {
        if (strcmp(entry->d_name, ".") == 0) continue;
        if (strcmp(entry->d_name, "..") == 0) continue;
        char full[4096];
        snprintf(full, sizeof(full), "%s/%s", dirpath, entry->d_name);
        nftw(full, maku_rmrf_callback, 16, FTW_DEPTH | FTW_PHYS);
    }
    closedir(d);
}

static int maku_journal_vacuum(const char *size_limit) {
    char *argv[4];
    argv[0] = "journalctl";
    argv[1] = "--vacuum-size";
    static char arg_buf[64];
    snprintf(arg_buf, sizeof(arg_buf), "%s", size_limit ? size_limit : "50M");
    argv[2] = arg_buf;
    argv[3] = NULL;

    pid_t pid = fork();
    if (pid < 0) return -1;
    if (pid == 0) {
        execvp(argv[0], argv);
        _exit(127);
    }
    int status = 0;
    waitpid(pid, &status, 0);
    return WIFEXITED(status) ? WEXITSTATUS(status) : -1;
}

static int maku_pkg_cache_clean(void) {
    const char *candidates[][3] = {
        { "dnf", "clean", "all" },
        { "apt-get", "clean", NULL },
        { "pacman", "-Scc", NULL },
        { NULL, NULL, NULL }
    };

    for (int i = 0; candidates[i][0] != NULL; i++) {
        char check_path[256];
        snprintf(check_path, sizeof(check_path), "/usr/bin/%s", candidates[i][0]);
        if (access(check_path, X_OK) != 0) continue;

        char *argv[5];
        int idx = 0;
        argv[idx++] = (char *)candidates[i][0];
        if (candidates[i][1]) argv[idx++] = (char *)candidates[i][1];
        if (candidates[i][2]) argv[idx++] = (char *)candidates[i][2];
        argv[idx++] = "-y";
        argv[idx] = NULL;

        pid_t pid = fork();
        if (pid < 0) continue;
        if (pid == 0) {
            execvp(argv[0], argv);
            _exit(127);
        }
        int status = 0;
        waitpid(pid, &status, 0);
        return WIFEXITED(status) ? WEXITSTATUS(status) : -1;
    }
    return -1;
}

static int maku_fstrim_all(void) {
    char *argv[3];
    argv[0] = "fstrim";
    argv[1] = "-a";
    argv[2] = NULL;

    pid_t pid = fork();
    if (pid < 0) return -1;
    if (pid == 0) {
        execvp(argv[0], argv);
        _exit(127);
    }
    int status = 0;
    waitpid(pid, &status, 0);
    return WIFEXITED(status) ? WEXITSTATUS(status) : -1;
}

static const char *maku_telemetry_domains[] = {
    "telemetry.fedoraproject.org",
    "countme.fedoraproject.org",
    "metrics.mozilla.com",
    "incoming.telemetry.mozilla.org",
    "vortex.data.microsoft.com",
    "watson.telemetry.microsoft.com",
    NULL
};

static const char *MAKU_HOSTS_MARKER_BEGIN = "# MAKUTWEAKER_TELEMETRY_BLOCK_BEGIN";
static const char *MAKU_HOSTS_MARKER_END = "# MAKUTWEAKER_TELEMETRY_BLOCK_END";

static int maku_hosts_block(void) {
    FILE *f = fopen(MAKU_HOSTS_FILE, "a");
    if (!f) return -1;
    fprintf(f, "\n%s\n", MAKU_HOSTS_MARKER_BEGIN);
    for (int i = 0; maku_telemetry_domains[i] != NULL; i++) {
        fprintf(f, "127.0.0.1 %s\n", maku_telemetry_domains[i]);
    }
    fprintf(f, "%s\n", MAKU_HOSTS_MARKER_END);
    fclose(f);
    return 0;
}

static int maku_hosts_unblock(void) {
    FILE *f = fopen(MAKU_HOSTS_FILE, "r");
    if (!f) return -1;

    char *content = NULL;
    size_t total = 0;
    char chunk[4096];
    size_t n;
    while ((n = fread(chunk, 1, sizeof(chunk), f)) > 0) {
        char *tmp = realloc(content, total + n + 1);
        if (!tmp) break;
        content = tmp;
        memcpy(content + total, chunk, n);
        total += n;
        content[total] = '\0';
    }
    fclose(f);
    if (!content) return -1;

    char *begin = strstr(content, MAKU_HOSTS_MARKER_BEGIN);
    char *end = strstr(content, MAKU_HOSTS_MARKER_END);
    if (begin && end && end > begin) {
        end += strlen(MAKU_HOSTS_MARKER_END);
        while (*end == '\n') end++;

        size_t prefix_len = (size_t)(begin - content);
        size_t suffix_len = strlen(end);

        char *rebuilt = malloc(prefix_len + suffix_len + 1);
        if (rebuilt) {
            memcpy(rebuilt, content, prefix_len);
            memcpy(rebuilt + prefix_len, end, suffix_len);
            rebuilt[prefix_len + suffix_len] = '\0';

            FILE *fw = fopen(MAKU_HOSTS_FILE, "w");
            if (fw) {
                fwrite(rebuilt, 1, strlen(rebuilt), fw);
                fclose(fw);
            }
            free(rebuilt);
        }
    }
    free(content);
    return 0;
}

int main(int argc, char **argv) {
    if (argc < 2) {
        fprintf(stderr, "usage: tweaker_backend --flag [arg]\n");
        return 1;
    }

    const char *flag = argv[1];
    const char *arg = argc > 2 ? argv[2] : NULL;

    if (strcmp(flag, "--set-cpu") == 0 && arg) {
        return maku_write_all_cpu_governors(arg) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--set-swappiness") == 0 && arg) {
        return maku_write_sysfile(MAKU_SYSCTL_SWAPPINESS, arg) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--set-file-max") == 0 && arg) {
        return maku_write_sysfile(MAKU_SYSCTL_FILEMAX, arg) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--set-split-lock") == 0 && arg) {
        return maku_write_sysfile(MAKU_SYSCTL_SPLITLOCK, arg) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--set-bbr") == 0 && arg) {
        if (strcmp(arg, "1") == 0) {
            int r1 = maku_write_sysfile(MAKU_SYSCTL_QDISC, "fq");
            int r2 = maku_write_sysfile(MAKU_SYSCTL_TCPCC, "bbr");
            return (r1 == 0 && r2 == 0) ? 0 : 1;
        } else {
            int r1 = maku_write_sysfile(MAKU_SYSCTL_QDISC, "pfifo_fast");
            int r2 = maku_write_sysfile(MAKU_SYSCTL_TCPCC, "cubic");
            return (r1 == 0 && r2 == 0) ? 0 : 1;
        }
    }
    if (strcmp(flag, "--service-enable") == 0 && arg) {
        return maku_service_control(arg, 1) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--service-disable") == 0 && arg) {
        return maku_service_control(arg, 0) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--journal-vacuum") == 0) {
        return maku_journal_vacuum(arg) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--clean-user-cache") == 0) {
        const char *sudo_user = getenv("SUDO_USER");
        const char *pkexec_uid_env = getenv("PKEXEC_UID");
        (void)pkexec_uid_env;
        char cache_path[4096];
        if (sudo_user) {
            snprintf(cache_path, sizeof(cache_path), "/home/%s/.cache", sudo_user);
        } else {
            snprintf(cache_path, sizeof(cache_path), "%s", "/root/.cache");
        }
        maku_rmrf_contents(cache_path);
        return 0;
    }
    if (strcmp(flag, "--clean-pkg-cache") == 0) {
        return maku_pkg_cache_clean() == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--fstrim") == 0) {
        return maku_fstrim_all() == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--drop-caches") == 0 && arg) {
        return maku_write_sysfile(MAKU_SYSCTL_DROPCACHES, arg) == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--hosts-block") == 0) {
        return maku_hosts_block() == 0 ? 0 : 1;
    }
    if (strcmp(flag, "--hosts-unblock") == 0) {
        return maku_hosts_unblock() == 0 ? 0 : 1;
    }

    fprintf(stderr, "unknown flag: %s\n", flag);
    return 1;
}
