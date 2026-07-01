#include "maku_common.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>

MakuInitSystem maku_detect_init_system(void) {
    struct stat st;
    if (stat(MAKU_INIT_SYSTEMD_DIR, &st) == 0 && S_ISDIR(st.st_mode)) {
        return MAKU_INIT_SYSTEMD;
    }
    if (stat(MAKU_INIT_OPENRC_FILE, &st) == 0) {
        return MAKU_INIT_OPENRC;
    }
    if (stat(MAKU_INIT_RUNIT_DIR, &st) == 0 && S_ISDIR(st.st_mode)) {
        return MAKU_INIT_RUNIT;
    }
    return MAKU_INIT_UNKNOWN;
}

int maku_write_sysfile(const char *path, const char *value) {
    FILE *f = fopen(path, "w");
    if (!f) {
        return -1;
    }
    size_t len = strlen(value);
    size_t written = fwrite(value, 1, len, f);
    fclose(f);
    if (written != len) {
        return -1;
    }
    return 0;
}

long maku_get_online_cpu_count(void) {
    long n = sysconf(_SC_NPROCESSORS_ONLN);
    if (n < 1) n = 1;
    return n;
}
