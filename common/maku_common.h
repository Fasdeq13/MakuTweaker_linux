#ifndef MAKU_COMMON_H
#define MAKU_COMMON_H

#define MAKU_APP_ID "org.maku.Tweaker"
#define MAKU_BACKEND_PATH "/usr/local/bin/tweaker_backend"

#define MAKU_PROC_CPUINFO "/proc/cpuinfo"
#define MAKU_PROC_MEMINFO "/proc/meminfo"
#define MAKU_PROC_UPTIME "/proc/uptime"
#define MAKU_PROC_STAT "/proc/stat"
#define MAKU_OS_RELEASE "/etc/os-release"

#define MAKU_CPU_GOVERNOR_FMT "/sys/devices/system/cpu/cpu%d/cpufreq/scaling_governor"
#define MAKU_CPU_CURFREQ_FMT "/sys/devices/system/cpu/cpu%d/cpufreq/scaling_cur_freq"
#define MAKU_CPU_ONLINE "/sys/devices/system/cpu/online"

#define MAKU_SYSCTL_SWAPPINESS "/proc/sys/vm/swappiness"
#define MAKU_SYSCTL_FILEMAX "/proc/sys/fs/file-max"
#define MAKU_SYSCTL_SPLITLOCK "/proc/sys/kernel/split_lock_mitigate"
#define MAKU_SYSCTL_QDISC "/proc/sys/net/core/default_qdisc"
#define MAKU_SYSCTL_TCPCC "/proc/sys/net/ipv4/tcp_congestion_control"
#define MAKU_SYSCTL_DROPCACHES "/proc/sys/vm/drop_caches"

#define MAKU_INIT_SYSTEMD_DIR "/run/systemd/system"
#define MAKU_INIT_OPENRC_FILE "/etc/init.d/openrc-shutdown"
#define MAKU_INIT_RUNIT_DIR "/etc/runit/runsvdir"

#define MAKU_HOSTS_FILE "/etc/hosts"

typedef enum {
    MAKU_INIT_UNKNOWN = 0,
    MAKU_INIT_SYSTEMD,
    MAKU_INIT_OPENRC,
    MAKU_INIT_RUNIT
} MakuInitSystem;

typedef enum {
    MAKU_CMD_NONE = 0,
    MAKU_CMD_SET_CPU_PERFORMANCE,
    MAKU_CMD_SET_CPU_POWERSAVE,
    MAKU_CMD_SET_SWAPPINESS,
    MAKU_CMD_SET_FILE_LIMITS,
    MAKU_CMD_SET_SPLIT_LOCK,
    MAKU_CMD_SET_BBR,
    MAKU_CMD_UNSET_BBR,
    MAKU_CMD_SERVICE_ENABLE,
    MAKU_CMD_SERVICE_DISABLE,
    MAKU_CMD_JOURNAL_VACUUM,
    MAKU_CMD_PKG_CACHE_CLEAN,
    MAKU_CMD_FSTRIM,
    MAKU_CMD_DROP_CACHES,
    MAKU_CMD_HOSTS_BLOCK,
    MAKU_CMD_HOSTS_UNBLOCK,
    MAKU_CMD_ABRT_DISABLE,
    MAKU_CMD_ABRT_ENABLE
} MakuBackendCommand;

MakuInitSystem maku_detect_init_system(void);
int maku_write_sysfile(const char *path, const char *value);
long maku_get_online_cpu_count(void);

#endif
