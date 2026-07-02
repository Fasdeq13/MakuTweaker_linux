#include <gtk/gtk.h>
#include "localization.h"
#include "maku_window.h"
#include "backend_bridge.h"
#include "maku_common.h"
#include "maku_state.h"

static void on_toggle_cups(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call(state ? "--service-enable" : "--service-disable", "cups");
    maku_state_set_bool(MAKU_STATE_KEY_SVC_CUPS, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_avahi(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call(state ? "--service-enable" : "--service-disable", "avahi-daemon");
    maku_state_set_bool(MAKU_STATE_KEY_SVC_AVAHI, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_samba(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call(state ? "--service-enable" : "--service-disable", "smb");
    maku_state_set_bool(MAKU_STATE_KEY_SVC_SAMBA, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_abrt(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call(state ? "--service-enable" : "--service-disable", "abrtd");
    maku_state_set_bool(MAKU_STATE_KEY_SVC_ABRT, state);
    gtk_switch_set_state(sw, state);
}

static const char *maku_init_name_str(MakuInitSystem init) {
    switch (init) {
        case MAKU_INIT_SYSTEMD: return "systemd";
        case MAKU_INIT_OPENRC: return "OpenRC";
        case MAKU_INIT_RUNIT: return "runit";
        default: return "unknown";
    }
}

GtkWidget *maku_build_menu_components(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    MakuInitSystem init = maku_detect_init_system();
    char subtitle[64];
    snprintf(subtitle, sizeof(subtitle), "init: %s", maku_init_name_str(init));
    GtkWidget *lbl_init = gtk_label_new(subtitle);
    gtk_widget_add_css_class(lbl_init, "maku-card-subtitle");
    gtk_label_set_xalign(GTK_LABEL(lbl_init), 0.0f);
    gtk_box_append(GTK_BOX(box), lbl_init);

    app->sw_cups = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_cups), maku_state_get_bool(MAKU_STATE_KEY_SVC_CUPS, TRUE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_cups), maku_state_get_bool(MAKU_STATE_KEY_SVC_CUPS, TRUE));
    g_signal_connect(app->sw_cups, "state-set", G_CALLBACK(on_toggle_cups), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SVC_CUPS), NULL, app->sw_cups));

    app->sw_avahi = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_avahi), maku_state_get_bool(MAKU_STATE_KEY_SVC_AVAHI, TRUE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_avahi), maku_state_get_bool(MAKU_STATE_KEY_SVC_AVAHI, TRUE));
    g_signal_connect(app->sw_avahi, "state-set", G_CALLBACK(on_toggle_avahi), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SVC_AVAHI), NULL, app->sw_avahi));

    app->sw_samba = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_samba), maku_state_get_bool(MAKU_STATE_KEY_SVC_SAMBA, TRUE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_samba), maku_state_get_bool(MAKU_STATE_KEY_SVC_SAMBA, TRUE));
    g_signal_connect(app->sw_samba, "state-set", G_CALLBACK(on_toggle_samba), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SVC_SAMBA), NULL, app->sw_samba));

    app->sw_abrt = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_abrt), maku_state_get_bool(MAKU_STATE_KEY_SVC_ABRT, TRUE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_abrt), maku_state_get_bool(MAKU_STATE_KEY_SVC_ABRT, TRUE));
    g_signal_connect(app->sw_abrt, "state-set", G_CALLBACK(on_toggle_abrt), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SVC_ABRT), NULL, app->sw_abrt));

    return box;
}
