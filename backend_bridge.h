#ifndef MAKU_BACKEND_BRIDGE_H
#define MAKU_BACKEND_BRIDGE_H

int maku_backend_call(const char *flag, const char *arg);
int maku_backend_call_pid(const char *flag, long pid);

#endif
