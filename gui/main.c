#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <stdlib.h>
#include <string.h>
#include "localization.h"
#include "maku_window.h"

static MakuAppWidgets g_app;

static void maku_load_css(void) {
    const char *candidates[] = {
        "/usr/local/share/makutweaker/style.css",
        "./styles/style.css",
        "../styles/style.css",
        NULL
    };

    const char *chosen = NULL;
    for (int i = 0; candidates[i] != NULL; i++) {
        if (g_file_test(candidates[i], G_FILE_TEST_EXISTS)) {
            chosen = candidates[i];
            break;
        }
    }

    if (!chosen) {
        return;
    }

    GtkCssProvider *provider = gtk_css_provider_new();
    gtk_css_provider_load_from_path(provider, chosen);
    gtk_style_context_add_provider_for_display(
        gdk_display_get_default(),
        GTK_STYLE_PROVIDER(provider),
        GTK_STYLE_PROVIDER_PRIORITY_APPLICATION
    );
    g_object_unref(provider);
}

static void maku_on_sidebar_row_selected(GtkListBox *box, GtkListBoxRow *row, gpointer user_data) {
    (void)box;
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;
    if (!row) return;
    gint index = gtk_list_box_row_get_index(row);
    const char *names[] = {
        "info", "perf", "components", "processes", "personalize",
        "debloat", "cleanup", "security", "filemgr", "recovery"
    };
    if (index >= 0 && index < 10) {
        gtk_stack_set_visible_child_name(GTK_STACK(app->content_stack), names[index]);
    }
}

static GtkWidget *maku_sidebar_row_new(const char *label_text) {
    GtkWidget *row = gtk_list_box_row_new();
    GtkWidget *label = gtk_label_new(label_text);
    gtk_label_set_xalign(GTK_LABEL(label), 0.0f);
    gtk_widget_set_margin_start(label, 4);
    gtk_widget_set_margin_end(label, 4);
    gtk_list_box_row_set_child(GTK_LIST_BOX_ROW(row), label);
    return row;
}

static void maku_build_sidebar(MakuAppWidgets *app) {
    app->sidebar_list = gtk_list_box_new();
    gtk_widget_add_css_class(app->sidebar_list, "sidebar");
    gtk_list_box_set_selection_mode(GTK_LIST_BOX(app->sidebar_list), GTK_SELECTION_SINGLE);

    const MakuStringId ids[] = {
        STR_MENU_INFO, STR_MENU_PERF, STR_MENU_COMPONENTS, STR_MENU_PROCESSES,
        STR_MENU_PERSONALIZE, STR_MENU_DEBLOAT, STR_MENU_CLEANUP,
        STR_MENU_SECURITY, STR_MENU_FILEMGR, STR_MENU_RECOVERY
    };
    for (int i = 0; i < 10; i++) {
        GtkWidget *row = maku_sidebar_row_new(maku_tr(ids[i]));
        gtk_list_box_append(GTK_LIST_BOX(app->sidebar_list), row);
    }

    g_signal_connect(app->sidebar_list, "row-selected", G_CALLBACK(maku_on_sidebar_row_selected), app);
}

static void maku_activate(GtkApplication *gtk_app, gpointer user_data) {
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;

    maku_load_css();

    app->window = gtk_application_window_new(gtk_app);
    gtk_window_set_title(GTK_WINDOW(app->window), maku_tr(STR_APP_TITLE));
    gtk_window_set_default_size(GTK_WINDOW(app->window), 1100, 700);

    GtkWidget *root = gtk_paned_new(GTK_ORIENTATION_HORIZONTAL);
    gtk_widget_set_hexpand(root, TRUE);
    gtk_widget_set_vexpand(root, TRUE);

    maku_build_sidebar(app);
    GtkWidget *sidebar_scroll = gtk_scrolled_window_new();
    gtk_scrolled_window_set_child(GTK_SCROLLED_WINDOW(sidebar_scroll), app->sidebar_list);
    gtk_widget_set_size_request(sidebar_scroll, 240, -1);

    app->content_stack = gtk_stack_new();
    gtk_stack_set_transition_type(GTK_STACK(app->content_stack), GTK_STACK_TRANSITION_TYPE_CROSSFADE);

    GtkWidget *page_info = maku_build_menu_info(app);
    GtkWidget *page_perf = maku_build_menu_perf(app);
    GtkWidget *page_components = maku_build_menu_components(app);
    GtkWidget *page_processes = maku_build_menu_processes(app);
    GtkWidget *page_personalize = maku_build_menu_personalize(app);
    GtkWidget *page_debloat = maku_build_menu_debloat(app);
    GtkWidget *page_cleanup = maku_build_menu_cleanup(app);
    GtkWidget *page_security = maku_build_menu_security(app);
    GtkWidget *page_filemgr = maku_build_menu_filemgr(app);
    GtkWidget *page_recovery = maku_build_menu_recovery(app);

    gtk_stack_add_named(GTK_STACK(app->content_stack), page_info, "info");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_perf, "perf");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_components, "components");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_processes, "processes");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_personalize, "personalize");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_debloat, "debloat");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_cleanup, "cleanup");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_security, "security");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_filemgr, "filemgr");
    gtk_stack_add_named(GTK_STACK(app->content_stack), page_recovery, "recovery");

    GtkWidget *content_scroll = gtk_scrolled_window_new();
    gtk_scrolled_window_set_child(GTK_SCROLLED_WINDOW(content_scroll), app->content_stack);

    gtk_paned_set_start_child(GTK_PANED(root), sidebar_scroll);
    gtk_paned_set_end_child(GTK_PANED(root), content_scroll);
    gtk_paned_set_resize_start_child(GTK_PANED(root), FALSE);
    gtk_paned_set_resize_end_child(GTK_PANED(root), TRUE);

    gtk_window_set_child(GTK_WINDOW(app->window), root);

    GtkListBoxRow *first_row = gtk_list_box_get_row_at_index(GTK_LIST_BOX(app->sidebar_list), 0);
    gtk_list_box_select_row(GTK_LIST_BOX(app->sidebar_list), first_row);

    app->info_timer_id = g_timeout_add_seconds(1, maku_info_timer_cb, app);

    gtk_window_present(GTK_WINDOW(app->window));
}

int main(int argc, char **argv) {
    const char *lang_env = getenv("MAKU_LANG");
    if (lang_env && strcmp(lang_env, "en") == 0) {
        maku_set_lang(LANG_EN);
    } else {
        maku_set_lang(LANG_RU);
    }

    memset(&g_app, 0, sizeof(g_app));

    GtkApplication *gtk_app = gtk_application_new("org.maku.Tweaker", G_APPLICATION_DEFAULT_FLAGS);
    g_signal_connect(gtk_app, "activate", G_CALLBACK(maku_activate), &g_app);
    int status = g_application_run(G_APPLICATION(gtk_app), argc, argv);
    g_object_unref(gtk_app);
    return status;
}
