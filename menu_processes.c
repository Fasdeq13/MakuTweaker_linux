#include <gtk/gtk.h>
#include <dirent.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <signal.h>
#include "localization.h"
#include "maku_window.h"

enum {
    COL_PID = 0,
    COL_NAME,
    COL_STATE,
    COL_N
};

static gboolean maku_is_all_digits(const char *s) {
    if (!s || !*s) return FALSE;
    for (const char *p = s; *p; p++) {
        if (!isdigit((unsigned char)*p)) return FALSE;
    }
    return TRUE;
}

static char *maku_read_proc_comm(long pid) {
    char path[64];
    snprintf(path, sizeof(path), "/proc/%ld/comm", pid);
    FILE *f = fopen(path, "r");
    if (!f) return strdup("?");
    char *line = NULL;
    size_t cap = 0;
    ssize_t len = getline(&line, &cap, f);
    fclose(f);
    if (len <= 0) {
        free(line);
        return strdup("?");
    }
    if (line[len - 1] == '\n') line[len - 1] = '\0';
    return line;
}

static char maku_read_proc_state(long pid) {
    char path[64];
    snprintf(path, sizeof(path), "/proc/%ld/stat", pid);
    FILE *f = fopen(path, "r");
    if (!f) return '?';
    char comm[512];
    char state = '?';
    long dummy_pid;
    if (fscanf(f, "%ld %511s %c", &dummy_pid, comm, &state) != 3) {
        state = '?';
    }
    fclose(f);
    return state;
}

static void maku_refresh_process_list(MakuAppWidgets *app) {
    gtk_list_store_clear(app->proc_store);

    DIR *d = opendir("/proc");
    if (!d) return;

    struct dirent *entry;
    while ((entry = readdir(d)) != NULL) {
        if (!maku_is_all_digits(entry->d_name)) continue;
        long pid = atol(entry->d_name);
        char *name = maku_read_proc_comm(pid);
        char state = maku_read_proc_state(pid);
        char state_str[2] = { state, '\0' };

        GtkTreeIter iter;
        gtk_list_store_append(app->proc_store, &iter);
        gtk_list_store_set(app->proc_store, &iter,
            COL_PID, (gint)pid,
            COL_NAME, name,
            COL_STATE, state_str,
            -1);
        free(name);
    }
    closedir(d);
}

static gboolean maku_get_selected_pid(MakuAppWidgets *app, long *out_pid) {
    GtkTreeSelection *sel = gtk_tree_view_get_selection(GTK_TREE_VIEW(app->proc_store_view));
    GtkTreeModel *model;
    GtkTreeIter iter;
    if (!gtk_tree_selection_get_selected(sel, &model, &iter)) {
        return FALSE;
    }
    gint pid = 0;
    gtk_tree_model_get(model, &iter, COL_PID, &pid, -1);
    *out_pid = (long)pid;
    return TRUE;
}

static void on_btn_kill(GtkButton *btn, gpointer user_data) {
    (void)btn;
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;
    long pid;
    if (maku_get_selected_pid(app, &pid)) {
        kill((pid_t)pid, SIGKILL);
        maku_refresh_process_list(app);
    }
}

static void on_btn_freeze(GtkButton *btn, gpointer user_data) {
    (void)btn;
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;
    long pid;
    if (maku_get_selected_pid(app, &pid)) {
        kill((pid_t)pid, SIGSTOP);
        maku_refresh_process_list(app);
    }
}

static void on_btn_unfreeze(GtkButton *btn, gpointer user_data) {
    (void)btn;
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;
    long pid;
    if (maku_get_selected_pid(app, &pid)) {
        kill((pid_t)pid, SIGCONT);
        maku_refresh_process_list(app);
    }
}

static gboolean on_refresh_timer(gpointer user_data) {
    maku_refresh_process_list((MakuAppWidgets *)user_data);
    return G_SOURCE_CONTINUE;
}

static const char *maku_static_services[] = {
    "NetworkManager", "sshd", "cups", "avahi-daemon", "smb", "bluetooth", NULL
};

static void maku_populate_service_list(MakuAppWidgets *app) {
    gtk_list_store_clear(app->svc_store);
    for (int i = 0; maku_static_services[i] != NULL; i++) {
        GtkTreeIter iter;
        gtk_list_store_append(app->svc_store, &iter);
        gtk_list_store_set(app->svc_store, &iter,
            COL_PID, i,
            COL_NAME, maku_static_services[i],
            COL_STATE, "active",
            -1);
    }
}

GtkWidget *maku_build_menu_processes(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);
    gtk_widget_set_vexpand(box, TRUE);

    app->proc_store = gtk_list_store_new(COL_N, G_TYPE_INT, G_TYPE_STRING, G_TYPE_STRING);
    app->proc_store_view = gtk_tree_view_new_with_model(GTK_TREE_MODEL(app->proc_store));

    GtkCellRenderer *r1 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->proc_store_view), -1, "PID", r1, "text", COL_PID, NULL);
    GtkCellRenderer *r2 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->proc_store_view), -1, "Name", r2, "text", COL_NAME, NULL);
    GtkCellRenderer *r3 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->proc_store_view), -1, "State", r3, "text", COL_STATE, NULL);

    GtkWidget *proc_scroll = gtk_scrolled_window_new();
    gtk_widget_set_size_request(proc_scroll, -1, 320);
    gtk_scrolled_window_set_child(GTK_SCROLLED_WINDOW(proc_scroll), app->proc_store_view);
    gtk_box_append(GTK_BOX(box), proc_scroll);

    GtkWidget *btn_box = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
    GtkWidget *btn_kill = gtk_button_new_with_label(maku_tr(STR_PROC_KILL));
    gtk_widget_add_css_class(btn_kill, "destructive-action");
    g_signal_connect(btn_kill, "clicked", G_CALLBACK(on_btn_kill), app);
    GtkWidget *btn_freeze = gtk_button_new_with_label(maku_tr(STR_PROC_FREEZE));
    g_signal_connect(btn_freeze, "clicked", G_CALLBACK(on_btn_freeze), app);
    GtkWidget *btn_unfreeze = gtk_button_new_with_label(maku_tr(STR_PROC_UNFREEZE));
    g_signal_connect(btn_unfreeze, "clicked", G_CALLBACK(on_btn_unfreeze), app);
    gtk_box_append(GTK_BOX(btn_box), btn_kill);
    gtk_box_append(GTK_BOX(btn_box), btn_freeze);
    gtk_box_append(GTK_BOX(btn_box), btn_unfreeze);
    gtk_box_append(GTK_BOX(box), btn_box);

    app->svc_store = gtk_list_store_new(COL_N, G_TYPE_INT, G_TYPE_STRING, G_TYPE_STRING);
    app->svc_store_view = gtk_tree_view_new_with_model(GTK_TREE_MODEL(app->svc_store));
    GtkCellRenderer *r4 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->svc_store_view), -1, "ID", r4, "text", COL_PID, NULL);
    GtkCellRenderer *r5 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->svc_store_view), -1, "Service", r5, "text", COL_NAME, NULL);
    GtkCellRenderer *r6 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->svc_store_view), -1, "Status", r6, "text", COL_STATE, NULL);

    GtkWidget *svc_scroll = gtk_scrolled_window_new();
    gtk_widget_set_size_request(svc_scroll, -1, 180);
    gtk_scrolled_window_set_child(GTK_SCROLLED_WINDOW(svc_scroll), app->svc_store_view);
    gtk_box_append(GTK_BOX(box), svc_scroll);

    maku_refresh_process_list(app);
    maku_populate_service_list(app);
    g_timeout_add_seconds(2, on_refresh_timer, app);

    return box;
}
