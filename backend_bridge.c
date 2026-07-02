#include "backend_bridge.h"
#include "maku_common.h"
#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include <sys/wait.h>
#include <sys/types.h>

static int maku_run_pkexec(char *const argv[]) {
    pid_t pid = fork();
    if (pid < 0) {
        return -1;
    }
    if (pid == 0) {
        execvp("pkexec", argv);
        _exit(127);
    }
    int status = 0;
    if (waitpid(pid, &status, 0) < 0) {
        return -1;
    }
    if (WIFEXITED(status)) {
        return WEXITSTATUS(status);
    }
    return -1;
}

int maku_backend_call(const char *flag, const char *arg) {
    char *argv[6];
    int idx = 0;
    argv[idx++] = "pkexec";
    argv[idx++] = MAKU_BACKEND_PATH;
    argv[idx++] = (char *)flag;
    if (arg) {
        argv[idx++] = (char *)arg;
    }
    argv[idx] = NULL;
    return maku_run_pkexec(argv);
}

int maku_backend_call_pid(const char *flag, long pid) {
    char pidbuf[32];
    snprintf(pidbuf, sizeof(pidbuf), "%ld", pid);
    return maku_backend_call(flag, pidbuf);
}
