#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <stdlib.h>
#include "localization.h"
#include "maku_window.h"
#include "backend_bridge.h"

static void on_toggle_clean_logs(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--journal-vacuum", "50M");
    }
    gtk_switch_set_state(sw, FALSE);
    gtk_switch_set_active(sw, FALSE);
}

static void on_toggle_clean_cache(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--clean-user-cache", NULL);
    }
    gtk_switch_set_state(sw, FALSE);
    gtk_switch_set_active(sw, FALSE);
}

static void on_toggle_clean_pkgcache(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    if (state) {
        maku_backend_call("--clean-pkg-cache", NULL);
    }
    gtk_switch_set_state(sw, FALSE);
    gtk_switch_set_active(sw, FALSE);
}

static void on_btn_trim(GtkButton *btn, gpointer user_data) {
    (void)btn;
    (void)user_data;
    maku_backend_call("--fstrim", NULL);
}

GtkWidget *maku_build_menu_cleanup(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    app->sw_clean_logs = gtk_switch_new();
    g_signal_connect(app->sw_clean_logs, "state-set", G_CALLBACK(on_toggle_clean_logs), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_CLEANUP_LOGS),
        "journalctl --vacuum-size=50M", app->sw_clean_logs));

    app->sw_clean_cache = gtk_switch_new();
    g_signal_connect(app->sw_clean_cache, "state-set", G_CALLBACK(on_toggle_clean_cache), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_CLEANUP_CACHE),
        "~/.cache/*", app->sw_clean_cache));

    app->sw_clean_pkgcache = gtk_switch_new();
    g_signal_connect(app->sw_clean_pkgcache, "state-set", G_CALLBACK(on_toggle_clean_pkgcache), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_CLEANUP_PKGCACHE),
        "dnf clean all", app->sw_clean_pkgcache));

    GtkWidget *btn_trim = gtk_button_new_with_label(maku_tr(STR_CLEANUP_TRIM));
    gtk_widget_add_css_class(btn_trim, "suggested-action");
    g_signal_connect(btn_trim, "clicked", G_CALLBACK(on_btn_trim), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_CLEANUP_TRIM),
        "fstrim -a", btn_trim));

    return box;
}
