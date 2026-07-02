#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <dirent.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <strings.h>
#include <sys/stat.h>
#include <time.h>
#include "localization.h"
#include "maku_window.h"
#include "maku_state.h"

typedef struct {
    char name[512];
    char full_path[4608];
    int is_dir;
    off_t size;
    time_t mtime;
} MakuFileEntry;

static gboolean g_fm_show_hidden = FALSE;

static int maku_compare_entries(const void *a, const void *b) {
    const MakuFileEntry *ea = (const MakuFileEntry *)a;
    const MakuFileEntry *eb = (const MakuFileEntry *)b;
    if (ea->is_dir != eb->is_dir) {
        return eb->is_dir - ea->is_dir;
    }
    return strcasecmp(ea->name, eb->name);
}

static void maku_format_size(off_t size, char *buf, size_t buflen) {
    if (size < 1024) {
        snprintf(buf, buflen, "%ld B", (long)size);
    } else if (size < 1024 * 1024) {
        snprintf(buf, buflen, "%.1f KB", size / 1024.0);
    } else if (size < 1024 * 1024 * 1024) {
        snprintf(buf, buflen, "%.1f MB", size / (1024.0 * 1024.0));
    } else {
        snprintf(buf, buflen, "%.1f GB", size / (1024.0 * 1024.0 * 1024.0));
    }
}

static GtkWidget *maku_build_file_row(const MakuFileEntry *entry) {
    GtkWidget *row = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
    gtk_widget_add_css_class(row, "maku-card");

    const char *icon_name = entry->is_dir ? "folder-symbolic" : "text-x-generic-symbolic";
    GtkWidget *icon = gtk_image_new_from_icon_name(icon_name);
    gtk_box_append(GTK_BOX(row), icon);

    GtkWidget *name_label = gtk_label_new(entry->name);
    gtk_label_set_xalign(GTK_LABEL(name_label), 0.0f);
    gtk_widget_set_hexpand(name_label, TRUE);
    gtk_box_append(GTK_BOX(row), name_label);

    if (!entry->is_dir) {
        char size_buf[32];
        maku_format_size(entry->size, size_buf, sizeof(size_buf));
        GtkWidget *size_label = gtk_label_new(size_buf);
        gtk_widget_add_css_class(size_label, "maku-card-subtitle");
        gtk_box_append(GTK_BOX(row), size_label);
    }

    char time_buf[64];
    struct tm tm_info;
    localtime_r(&entry->mtime, &tm_info);
    strftime(time_buf, sizeof(time_buf), "%Y-%m-%d %H:%M", &tm_info);
    GtkWidget *time_label = gtk_label_new(time_buf);
    gtk_widget_add_css_class(time_label, "maku-card-subtitle");
    gtk_box_append(GTK_BOX(row), time_label);

    return row;
}

static void maku_refresh_file_list(MakuAppWidgets *app) {
    GtkWidget *child;
    while ((child = gtk_widget_get_first_child(app->fm_flowbox)) != NULL) {
        gtk_flow_box_remove(GTK_FLOW_BOX(app->fm_flowbox), child);
    }

    DIR *d = opendir(app->fm_current_path);
    if (!d) return;

    size_t capacity = 256;
    MakuFileEntry *entries = malloc(capacity * sizeof(MakuFileEntry));
    if (!entries) {
        closedir(d);
        return;
    }
    size_t count = 0;

    struct dirent *de;
    while ((de = readdir(d)) != NULL) {
        if (strcmp(de->d_name, ".") == 0) continue;
        if (strcmp(de->d_name, "..") == 0) continue;
        if (de->d_name[0] == '.' && !g_fm_show_hidden) continue;

        if (count == capacity) {
            size_t new_capacity = capacity * 2;
            MakuFileEntry *grown = realloc(entries, new_capacity * sizeof(MakuFileEntry));
            if (!grown) {
                break;
            }
            entries = grown;
            capacity = new_capacity;
        }

        MakuFileEntry *e = &entries[count];
        snprintf(e->name, sizeof(e->name), "%s", de->d_name);
        snprintf(e->full_path, sizeof(e->full_path), "%s/%s", app->fm_current_path, de->d_name);

        struct stat st;
        if (stat(e->full_path, &st) == 0) {
            e->is_dir = S_ISDIR(st.st_mode);
            e->size = st.st_size;
            e->mtime = st.st_mtime;
        } else {
            e->is_dir = 0;
            e->size = 0;
            e->mtime = 0;
        }
        count++;
    }
    closedir(d);

    qsort(entries, count, sizeof(MakuFileEntry), maku_compare_entries);

    for (size_t i = 0; i < count; i++) {
        GtkWidget *row = maku_build_file_row(&entries[i]);
        gtk_flow_box_append(GTK_FLOW_BOX(app->fm_flowbox), row);
    }

    free(entries);
}

static void on_toggle_hidden(GtkCheckButton *btn, gpointer user_data) {
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;
    g_fm_show_hidden = gtk_check_button_get_active(btn);
    maku_state_set_bool(MAKU_STATE_KEY_FM_HIDDEN, g_fm_show_hidden);
    maku_refresh_file_list(app);
}

GtkWidget *maku_build_menu_filemgr(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);
    gtk_widget_set_vexpand(box, TRUE);

    const char *home = getenv("HOME");
    snprintf(app->fm_current_path, sizeof(app->fm_current_path), "%s", home ? home : "/");

    g_fm_show_hidden = maku_state_get_bool(MAKU_STATE_KEY_FM_HIDDEN, FALSE);

    GtkWidget *toolbar = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
    app->fm_hidden_toggle = gtk_check_button_new_with_label(maku_tr(STR_FM_HIDDEN));
    gtk_check_button_set_active(GTK_CHECK_BUTTON(app->fm_hidden_toggle), g_fm_show_hidden);
    g_signal_connect(app->fm_hidden_toggle, "toggled", G_CALLBACK(on_toggle_hidden), app);
    gtk_box_append(GTK_BOX(toolbar), app->fm_hidden_toggle);
    gtk_box_append(GTK_BOX(box), toolbar);

    app->fm_flowbox = gtk_flow_box_new();
    gtk_flow_box_set_selection_mode(GTK_FLOW_BOX(app->fm_flowbox), GTK_SELECTION_SINGLE);
    gtk_flow_box_set_max_children_per_line(GTK_FLOW_BOX(app->fm_flowbox), 1);
    gtk_flow_box_set_homogeneous(GTK_FLOW_BOX(app->fm_flowbox), TRUE);

    GtkWidget *scroll = gtk_scrolled_window_new();
    gtk_widget_set_vexpand(scroll, TRUE);
    gtk_scrolled_window_set_child(GTK_SCROLLED_WINDOW(scroll), app->fm_flowbox);
    gtk_box_append(GTK_BOX(box), scroll);

    maku_refresh_file_list(app);

    return box;
}
