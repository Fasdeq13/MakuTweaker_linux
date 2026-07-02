#ifndef MAKU_STATE_H
#define MAKU_STATE_H

#include <gtk/gtk.h>

#define MAKU_STATE_KEY_MAX_CPU "perf_max_cpu"
#define MAKU_STATE_KEY_SWAPPINESS "perf_swappiness"
#define MAKU_STATE_KEY_FILE_LIMITS "perf_file_limits"
#define MAKU_STATE_KEY_SPLIT_LOCK "perf_split_lock"
#define MAKU_STATE_KEY_BBR "perf_bbr"

#define MAKU_STATE_KEY_SVC_CUPS "svc_cups"
#define MAKU_STATE_KEY_SVC_AVAHI "svc_avahi"
#define MAKU_STATE_KEY_SVC_SAMBA "svc_samba"
#define MAKU_STATE_KEY_SVC_ABRT "svc_abrt"

#define MAKU_STATE_KEY_DARK_THEME "personalize_dark_theme"
#define MAKU_STATE_KEY_ACCENT_COLOR "personalize_accent_color"

#define MAKU_STATE_KEY_FEDORA_BLOCK "sec_fedora_block"
#define MAKU_STATE_KEY_HOSTS_BLOCK "sec_hosts_block"

#define MAKU_STATE_KEY_FM_HIDDEN "filemgr_show_hidden"

#define MAKU_STATE_KEY_GNOME_HOT_CORNERS "gnome_hot_corners"
#define MAKU_STATE_KEY_GNOME_NAUTILUS_COUNT "gnome_nautilus_count"
#define MAKU_STATE_KEY_GNOME_FRACTIONAL_SCALING "gnome_fractional_scaling"
#define MAKU_STATE_KEY_GNOME_ANIMATIONS "gnome_animations"
#define MAKU_STATE_KEY_GNOME_NIGHT_LIGHT "gnome_night_light"
#define MAKU_STATE_KEY_GNOME_BATTERY_PERCENT "gnome_battery_percent"

void maku_state_load(void);
void maku_state_save(void);

gboolean maku_state_get_bool(const char *key, gboolean default_value);
void maku_state_set_bool(const char *key, gboolean value);

const char *maku_state_get_string(const char *key, const char *default_value);
void maku_state_set_string(const char *key, const char *value);

#endif
