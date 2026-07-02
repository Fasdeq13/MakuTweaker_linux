#include <gtk/gtk.h>
#include "localization.h"
#include "maku_window.h"
#include "backend_bridge.h"
#include "maku_state.h"

static void on_toggle_max_cpu(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call("--set-cpu", state ? "performance" : "powersave");
    maku_state_set_bool(MAKU_STATE_KEY_MAX_CPU, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_swappiness(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call("--set-swappiness", state ? "10" : "60");
    maku_state_set_bool(MAKU_STATE_KEY_SWAPPINESS, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_file_limits(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call("--set-file-max", state ? "2097152" : "9223372036854775807");
    maku_state_set_bool(MAKU_STATE_KEY_FILE_LIMITS, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_split_lock(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call("--set-split-lock", state ? "0" : "1");
    maku_state_set_bool(MAKU_STATE_KEY_SPLIT_LOCK, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_bbr(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call("--set-bbr", state ? "1" : "0");
    maku_state_set_bool(MAKU_STATE_KEY_BBR, state);
    gtk_switch_set_state(sw, state);
}

GtkWidget *maku_build_menu_perf(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    app->sw_max_cpu = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_max_cpu), maku_state_get_bool(MAKU_STATE_KEY_MAX_CPU, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_max_cpu), maku_state_get_bool(MAKU_STATE_KEY_MAX_CPU, FALSE));
    g_signal_connect(app->sw_max_cpu, "state-set", G_CALLBACK(on_toggle_max_cpu), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_MAX_CPU),
        "scaling_governor -> performance / powersave", app->sw_max_cpu));

    app->sw_swappiness = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_swappiness), maku_state_get_bool(MAKU_STATE_KEY_SWAPPINESS, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_swappiness), maku_state_get_bool(MAKU_STATE_KEY_SWAPPINESS, FALSE));
    g_signal_connect(app->sw_swappiness, "state-set", G_CALLBACK(on_toggle_swappiness), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_SWAPPINESS),
        "/proc/sys/vm/swappiness = 10", app->sw_swappiness));

    app->sw_file_limits = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_file_limits), maku_state_get_bool(MAKU_STATE_KEY_FILE_LIMITS, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_file_limits), maku_state_get_bool(MAKU_STATE_KEY_FILE_LIMITS, FALSE));
    g_signal_connect(app->sw_file_limits, "state-set", G_CALLBACK(on_toggle_file_limits), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_FILE_LIMITS),
        "/proc/sys/fs/file-max = 2097152", app->sw_file_limits));

    app->sw_split_lock = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_split_lock), maku_state_get_bool(MAKU_STATE_KEY_SPLIT_LOCK, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_split_lock), maku_state_get_bool(MAKU_STATE_KEY_SPLIT_LOCK, FALSE));
    g_signal_connect(app->sw_split_lock, "state-set", G_CALLBACK(on_toggle_split_lock), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_SPLIT_LOCK),
        "/proc/sys/kernel/split_lock_mitigate = 0", app->sw_split_lock));

    app->sw_bbr = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_bbr), maku_state_get_bool(MAKU_STATE_KEY_BBR, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_bbr), maku_state_get_bool(MAKU_STATE_KEY_BBR, FALSE));
    g_signal_connect(app->sw_bbr, "state-set", G_CALLBACK(on_toggle_bbr), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_BBR),
        "tcp_congestion_control = bbr, default_qdisc = fq", app->sw_bbr));

    return box;
}
