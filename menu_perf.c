#include <gtk/gtk.h>
#include "localization.h"
#include "maku_window.h"
#include "backend_bridge.h"

static void on_toggle_max_cpu(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--set-cpu", "performance");
    } else {
        maku_backend_call("--set-cpu", "powersave");
    }
    gtk_switch_set_state(sw, state);
}

static void on_toggle_swappiness(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--set-swappiness", "10");
    } else {
        maku_backend_call("--set-swappiness", "60");
    }
    gtk_switch_set_state(sw, state);
}

static void on_toggle_file_limits(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--set-file-max", "2097152");
    } else {
        maku_backend_call("--set-file-max", "9223372036854775807");
    }
    gtk_switch_set_state(sw, state);
}

static void on_toggle_split_lock(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--set-split-lock", "0");
    } else {
        maku_backend_call("--set-split-lock", "1");
    }
    gtk_switch_set_state(sw, state);
}

static void on_toggle_bbr(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--set-bbr", "1");
    } else {
        maku_backend_call("--set-bbr", "0");
    }
    gtk_switch_set_state(sw, state);
}

GtkWidget *maku_build_menu_perf(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    app->sw_max_cpu = gtk_switch_new();
    g_signal_connect(app->sw_max_cpu, "state-set", G_CALLBACK(on_toggle_max_cpu), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_MAX_CPU),
        "scaling_governor -> performance / powersave", app->sw_max_cpu));

    app->sw_swappiness = gtk_switch_new();
    g_signal_connect(app->sw_swappiness, "state-set", G_CALLBACK(on_toggle_swappiness), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_SWAPPINESS),
        "/proc/sys/vm/swappiness = 10", app->sw_swappiness));

    app->sw_file_limits = gtk_switch_new();
    g_signal_connect(app->sw_file_limits, "state-set", G_CALLBACK(on_toggle_file_limits), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_FILE_LIMITS),
        "/proc/sys/fs/file-max = 2097152", app->sw_file_limits));

    app->sw_split_lock = gtk_switch_new();
    g_signal_connect(app->sw_split_lock, "state-set", G_CALLBACK(on_toggle_split_lock), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_SPLIT_LOCK),
        "/proc/sys/kernel/split_lock_mitigate = 0", app->sw_split_lock));

    app->sw_bbr = gtk_switch_new();
    g_signal_connect(app->sw_bbr, "state-set", G_CALLBACK(on_toggle_bbr), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_TOGGLE_BBR),
        "tcp_congestion_control = bbr, default_qdisc = fq", app->sw_bbr));

    return box;
}
