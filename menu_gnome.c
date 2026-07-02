#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/wait.h>
#include "localization.h"
#include "maku_window.h"
#include "maku_state.h"

static gboolean maku_is_gnome_session(void) {
    const char *desktop = getenv("XDG_CURRENT_DESKTOP");
    if (desktop && strstr(desktop, "GNOME") != NULL) {
        return TRUE;
    }
    const char *session = getenv("DESKTOP_SESSION");
    if (session && strstr(session, "gnome") != NULL) {
        return TRUE;
    }
    return FALSE;
}

static int maku_gsettings_set(const char *schema, const char *key, const char *value) {
    char *argv[6];
    argv[0] = "gsettings";
    argv[1] = "set";
    argv[2] = (char *)schema;
    argv[3] = (char *)key;
    argv[4] = (char *)value;
    argv[5] = NULL;

    pid_t pid = fork();
    if (pid < 0) return -1;
    if (pid == 0) {
        execvp("gsettings", argv);
        _exit(127);
    }
    int status = 0;
    if (waitpid(pid, &status, 0) < 0) return -1;
    return WIFEXITED(status) ? WEXITSTATUS(status) : -1;
}

static void on_toggle_nautilus_count(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_gsettings_set("org.gnome.nautilus.preferences", "show-directory-item-counts",
        state ? "always" : "local-only");
    maku_state_set_bool(MAKU_STATE_KEY_GNOME_NAUTILUS_COUNT, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_fractional_scaling(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_gsettings_set("org.gnome.mutter", "experimental-features",
        state ? "['scale-monitor-framebuffer']" : "[]");
    maku_state_set_bool(MAKU_STATE_KEY_GNOME_FRACTIONAL_SCALING, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_animations(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_gsettings_set("org.gnome.desktop.interface", "enable-animations",
        state ? "true" : "false");
    maku_state_set_bool(MAKU_STATE_KEY_GNOME_ANIMATIONS, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_night_light(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_gsettings_set("org.gnome.settings-daemon.plugins.color", "night-light-enabled",
        state ? "true" : "false");
    maku_state_set_bool(MAKU_STATE_KEY_GNOME_NIGHT_LIGHT, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_battery_percent(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_gsettings_set("org.gnome.desktop.interface", "show-battery-percentage",
        state ? "true" : "false");
    maku_state_set_bool(MAKU_STATE_KEY_GNOME_BATTERY_PERCENT, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_hot_corners(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_gsettings_set("org.gnome.desktop.interface", "enable-hot-corners",
        state ? "true" : "false");
    maku_state_set_bool(MAKU_STATE_KEY_GNOME_HOT_CORNERS, state);
    gtk_switch_set_state(sw, state);
}

GtkWidget *maku_build_menu_gnome(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    if (!maku_is_gnome_session()) {
        GtkWidget *warning_card = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
        gtk_widget_add_css_class(warning_card, "maku-card");
        GtkWidget *warning_label = gtk_label_new(maku_tr(STR_GNOME_NOT_DETECTED));
        gtk_widget_add_css_class(warning_label, "maku-warning");
        gtk_label_set_wrap(GTK_LABEL(warning_label), TRUE);
        gtk_box_append(GTK_BOX(warning_card), warning_label);
        gtk_box_append(GTK_BOX(box), warning_card);
        return box;
    }

    app->sw_gnome_hot_corners = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_gnome_hot_corners),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_HOT_CORNERS, TRUE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_gnome_hot_corners),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_HOT_CORNERS, TRUE));
    g_signal_connect(app->sw_gnome_hot_corners, "state-set", G_CALLBACK(on_toggle_hot_corners), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_GNOME_HOT_CORNERS),
        "org.gnome.desktop.interface enable-hot-corners", app->sw_gnome_hot_corners));

    app->sw_gnome_nautilus_count = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_gnome_nautilus_count),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_NAUTILUS_COUNT, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_gnome_nautilus_count),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_NAUTILUS_COUNT, FALSE));
    g_signal_connect(app->sw_gnome_nautilus_count, "state-set", G_CALLBACK(on_toggle_nautilus_count), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_GNOME_NAUTILUS_COUNT),
        "org.gnome.nautilus.preferences show-directory-item-counts", app->sw_gnome_nautilus_count));

    app->sw_gnome_fractional_scaling = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_gnome_fractional_scaling),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_FRACTIONAL_SCALING, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_gnome_fractional_scaling),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_FRACTIONAL_SCALING, FALSE));
    g_signal_connect(app->sw_gnome_fractional_scaling, "state-set", G_CALLBACK(on_toggle_fractional_scaling), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_GNOME_FRACTIONAL_SCALING),
        "org.gnome.mutter experimental-features (scale-monitor-framebuffer)", app->sw_gnome_fractional_scaling));

    app->sw_gnome_animations = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_gnome_animations),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_ANIMATIONS, TRUE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_gnome_animations),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_ANIMATIONS, TRUE));
    g_signal_connect(app->sw_gnome_animations, "state-set", G_CALLBACK(on_toggle_animations), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_GNOME_ANIMATIONS),
        "org.gnome.desktop.interface enable-animations", app->sw_gnome_animations));

    app->sw_gnome_night_light = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_gnome_night_light),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_NIGHT_LIGHT, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_gnome_night_light),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_NIGHT_LIGHT, FALSE));
    g_signal_connect(app->sw_gnome_night_light, "state-set", G_CALLBACK(on_toggle_night_light), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_GNOME_NIGHT_LIGHT),
        "org.gnome.settings-daemon.plugins.color night-light-enabled", app->sw_gnome_night_light));

    app->sw_gnome_battery_percent = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_gnome_battery_percent),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_BATTERY_PERCENT, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_gnome_battery_percent),
        maku_state_get_bool(MAKU_STATE_KEY_GNOME_BATTERY_PERCENT, FALSE));
    g_signal_connect(app->sw_gnome_battery_percent, "state-set", G_CALLBACK(on_toggle_battery_percent), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_GNOME_SHOW_BATTERY_PERCENT),
        "org.gnome.desktop.interface show-battery-percentage", app->sw_gnome_battery_percent));

    return box;
}
