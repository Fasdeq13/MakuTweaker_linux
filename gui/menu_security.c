#include <gtk/gtk.h>
#include <stdio.h>
#include <stdlib.h>
#include "localization.h"
#include "maku_window.h"
#include "backend_bridge.h"
#include "maku_state.h"

static void on_toggle_fedora_block(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--service-disable", "abrt");
        maku_backend_call("--service-disable", "fedora-welcome");
    } else {
        maku_backend_call("--service-enable", "abrt");
        maku_backend_call("--service-enable", "fedora-welcome");
    }
    maku_state_set_bool(MAKU_STATE_KEY_FEDORA_BLOCK, state);
    gtk_switch_set_state(sw, state);
}

static void on_toggle_hosts_block(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    maku_backend_call(state ? "--hosts-block" : "--hosts-unblock", NULL);
    maku_state_set_bool(MAKU_STATE_KEY_HOSTS_BLOCK, state);
    gtk_switch_set_state(sw, state);
}

static void on_btn_clear_history(GtkButton *btn, gpointer user_data) {
    (void)btn;
    (void)user_data;
    const char *home = getenv("HOME");
    if (!home) return;

    char path[4096];
    snprintf(path, sizeof(path), "%s/.local/share/recently-used.xbel", home);

    FILE *f = fopen(path, "w");
    if (f) {
        fprintf(f, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<xbel version=\"1.0\"></xbel>\n");
        fclose(f);
    }
}

GtkWidget *maku_build_menu_security(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    app->sw_fedora_block = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_fedora_block), maku_state_get_bool(MAKU_STATE_KEY_FEDORA_BLOCK, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_fedora_block), maku_state_get_bool(MAKU_STATE_KEY_FEDORA_BLOCK, FALSE));
    g_signal_connect(app->sw_fedora_block, "state-set", G_CALLBACK(on_toggle_fedora_block), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SEC_FEDORA),
        "abrt.service, fedora-welcome.service", app->sw_fedora_block));

    app->sw_hosts_block = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_hosts_block), maku_state_get_bool(MAKU_STATE_KEY_HOSTS_BLOCK, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_hosts_block), maku_state_get_bool(MAKU_STATE_KEY_HOSTS_BLOCK, FALSE));
    g_signal_connect(app->sw_hosts_block, "state-set", G_CALLBACK(on_toggle_hosts_block), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SEC_HOSTS),
        "/etc/hosts -> 127.0.0.1", app->sw_hosts_block));

    GtkWidget *btn_history = gtk_button_new_with_label(maku_tr(STR_SEC_HISTORY));
    gtk_widget_add_css_class(btn_history, "destructive-action");
    g_signal_connect(btn_history, "clicked", G_CALLBACK(on_btn_clear_history), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_SEC_HISTORY),
        "~/.local/share/recently-used.xbel", btn_history));

    return box;
}
