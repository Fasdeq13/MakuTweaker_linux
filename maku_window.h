#ifndef MAKU_WINDOW_H
#define MAKU_WINDOW_H

#include <gtk/gtk.h>

typedef struct {
    GtkWidget *window;
    GtkWidget *sidebar_list;
    GtkWidget *content_stack;

    GtkWidget *lbl_distro;
    GtkWidget *lbl_session;
    GtkWidget *lbl_cpu_name;
    GtkWidget *lbl_cpu_freq;
    GtkWidget *lbl_uptime;
    GtkWidget *bar_ram;
    GtkWidget *lbl_ram_text;

    GtkWidget *sw_max_cpu;
    GtkWidget *sw_swappiness;
    GtkWidget *sw_file_limits;
    GtkWidget *sw_split_lock;
    GtkWidget *sw_bbr;

    GtkWidget *sw_cups;
    GtkWidget *sw_avahi;
    GtkWidget *sw_samba;
    GtkWidget *sw_abrt;

    GtkWidget *proc_store_view;
    GtkListStore *proc_store;
    GtkWidget *svc_store_view;
    GtkListStore *svc_store;

    GtkWidget *sw_dark_theme;
    GtkWidget *btn_pick_color;

    GtkWidget *app_store_view;
    GtkListStore *app_store;

    GtkWidget *sw_clean_logs;
    GtkWidget *sw_clean_cache;
    GtkWidget *sw_clean_pkgcache;

    GtkWidget *sw_fedora_block;
    GtkWidget *sw_hosts_block;

    GtkWidget *fm_hidden_toggle;
    GtkWidget *fm_flowbox;
    char fm_current_path[4096];

    guint info_timer_id;
} MakuAppWidgets;

GtkWidget *maku_build_menu_info(MakuAppWidgets *app);
GtkWidget *maku_build_menu_perf(MakuAppWidgets *app);
GtkWidget *maku_build_menu_components(MakuAppWidgets *app);
GtkWidget *maku_build_menu_processes(MakuAppWidgets *app);
GtkWidget *maku_build_menu_personalize(MakuAppWidgets *app);
GtkWidget *maku_build_menu_debloat(MakuAppWidgets *app);
GtkWidget *maku_build_menu_cleanup(MakuAppWidgets *app);
GtkWidget *maku_build_menu_security(MakuAppWidgets *app);
GtkWidget *maku_build_menu_filemgr(MakuAppWidgets *app);
GtkWidget *maku_build_menu_recovery(MakuAppWidgets *app);

GtkWidget *maku_make_card(const char *title, const char *subtitle, GtkWidget *control);
gboolean maku_info_timer_cb(gpointer user_data);

#endif
