#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <dirent.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <ftw.h>
#include "localization.h"
#include "maku_window.h"

typedef struct {
    char name[256];
    char icon[256];
    char exec[512];
    char categories[256];
    char desktop_path[4096];
    int broken;
} MakuDesktopEntry;

enum {
    APP_COL_NAME = 0,
    APP_COL_CATEGORY,
    APP_COL_STATUS,
    APP_COL_PATH,
    APP_COL_N
};

static void maku_trim_newline(char *s) {
    size_t len = strlen(s);
    while (len > 0 && (s[len - 1] == '\n' || s[len - 1] == '\r')) {
        s[--len] = '\0';
    }
}

static int maku_binary_exists_in_path(const char *exec_line) {
    char first_token[512];
    size_t i = 0;
    while (exec_line[i] && exec_line[i] != ' ' && i < sizeof(first_token) - 1) {
        first_token[i] = exec_line[i];
        i++;
    }
    first_token[i] = '\0';

    if (first_token[0] == '/') {
        return access(first_token, X_OK) == 0;
    }

    const char *path_env = getenv("PATH");
    if (!path_env) return 0;

    char *path_copy = strdup(path_env);
    if (!path_copy) return 0;

    int found = 0;
    char *saveptr = NULL;
    char *dir = strtok_r(path_copy, ":", &saveptr);
    while (dir) {
        char full[4096];
        snprintf(full, sizeof(full), "%s/%s", dir, first_token);
        if (access(full, X_OK) == 0) {
            found = 1;
            break;
        }
        dir = strtok_r(NULL, ":", &saveptr);
    }
    free(path_copy);
    return found;
}

static int maku_parse_desktop_file(const char *path, MakuDesktopEntry *out) {
    FILE *f = fopen(path, "r");
    if (!f) return -1;

    memset(out, 0, sizeof(*out));
    snprintf(out->desktop_path, sizeof(out->desktop_path), "%s", path);

    char *line = NULL;
    size_t cap = 0;
    ssize_t len;
    while ((len = getline(&line, &cap, f)) != -1) {
        maku_trim_newline(line);
        if (strncmp(line, "Name=", 5) == 0 && out->name[0] == '\0') {
            snprintf(out->name, sizeof(out->name), "%s", line + 5);
        } else if (strncmp(line, "Icon=", 5) == 0 && out->icon[0] == '\0') {
            snprintf(out->icon, sizeof(out->icon), "%s", line + 5);
        } else if (strncmp(line, "Exec=", 5) == 0 && out->exec[0] == '\0') {
            snprintf(out->exec, sizeof(out->exec), "%s", line + 5);
        } else if (strncmp(line, "Categories=", 11) == 0 && out->categories[0] == '\0') {
            snprintf(out->categories, sizeof(out->categories), "%s", line + 11);
        }
    }
    free(line);
    fclose(f);

    if (out->exec[0] != '\0') {
        out->broken = !maku_binary_exists_in_path(out->exec);
    } else {
        out->broken = 1;
    }

    return out->name[0] != '\0' ? 0 : -1;
}

static void maku_scan_directory_desktop(const char *dirpath, MakuAppWidgets *app) {
    DIR *d = opendir(dirpath);
    if (!d) return;

    struct dirent *entry;
    while ((entry = readdir(d)) != NULL) {
        size_t nlen = strlen(entry->d_name);
        if (nlen < 9) continue;
        if (strcmp(entry->d_name + nlen - 8, ".desktop") != 0) continue;

        char full[4096];
        snprintf(full, sizeof(full), "%s/%s", dirpath, entry->d_name);

        MakuDesktopEntry app_entry;
        if (maku_parse_desktop_file(full, &app_entry) == 0) {
            const char *category = app_entry.categories[0] ? app_entry.categories : "Other";
            const char *status = app_entry.broken ? maku_tr(STR_DEBLOAT_BROKEN) : "OK";

            GtkTreeIter iter;
            gtk_list_store_append(app->app_store, &iter);
            gtk_list_store_set(app->app_store, &iter,
                APP_COL_NAME, app_entry.name,
                APP_COL_CATEGORY, category,
                APP_COL_STATUS, status,
                APP_COL_PATH, app_entry.desktop_path,
                -1);
        }
    }
    closedir(d);
}

static void maku_refresh_app_list(MakuAppWidgets *app) {
    gtk_list_store_clear(app->app_store);

    maku_scan_directory_desktop("/usr/share/applications", app);

    const char *home = getenv("HOME");
    if (home) {
        char local_apps[4096];
        snprintf(local_apps, sizeof(local_apps), "%s/.local/share/applications", home);
        maku_scan_directory_desktop(local_apps, app);
    }
}

static int maku_rmrf_callback(const char *fpath, const struct stat *sb, int typeflag, struct FTW *ftwbuf) {
    (void)sb;
    (void)ftwbuf;
    if (typeflag == FTW_DP || typeflag == FTW_D) {
        rmdir(fpath);
    } else {
        unlink(fpath);
    }
    return 0;
}

static void maku_rmrf(const char *path) {
    nftw(path, maku_rmrf_callback, 16, FTW_DEPTH | FTW_PHYS);
}

static void maku_remove_app_with_roots(const char *app_name, const char *desktop_path) {
    unlink(desktop_path);

    const char *home = getenv("HOME");
    if (!home) return;

    const char *subdirs[] = { ".config", ".local/share", ".cache", NULL };
    for (int i = 0; subdirs[i] != NULL; i++) {
        char target[4096];
        snprintf(target, sizeof(target), "%s/%s/%s", home, subdirs[i], app_name);
        maku_rmrf(target);
    }
}

static void on_btn_remove_app(GtkButton *btn, gpointer user_data) {
    (void)btn;
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;

    GtkTreeSelection *sel = gtk_tree_view_get_selection(GTK_TREE_VIEW(app->app_store_view));
    GtkTreeModel *model;
    GtkTreeIter iter;
    if (!gtk_tree_selection_get_selected(sel, &model, &iter)) {
        return;
    }

    gchar *name = NULL;
    gchar *path = NULL;
    gtk_tree_model_get(model, &iter, APP_COL_NAME, &name, APP_COL_PATH, &path, -1);

    if (name && path) {
        maku_remove_app_with_roots(name, path);
    }

    g_free(name);
    g_free(path);

    maku_refresh_app_list(app);
}

GtkWidget *maku_build_menu_debloat(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);
    gtk_widget_set_vexpand(box, TRUE);

    app->app_store = gtk_list_store_new(APP_COL_N, G_TYPE_STRING, G_TYPE_STRING, G_TYPE_STRING, G_TYPE_STRING);
    app->app_store_view = gtk_tree_view_new_with_model(GTK_TREE_MODEL(app->app_store));

    GtkCellRenderer *r1 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->app_store_view), -1, "Name", r1, "text", APP_COL_NAME, NULL);
    GtkCellRenderer *r2 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->app_store_view), -1, "Category", r2, "text", APP_COL_CATEGORY, NULL);
    GtkCellRenderer *r3 = gtk_cell_renderer_text_new();
    gtk_tree_view_insert_column_with_attributes(GTK_TREE_VIEW(app->app_store_view), -1, "Status", r3, "text", APP_COL_STATUS, NULL);

    GtkWidget *scroll = gtk_scrolled_window_new();
    gtk_widget_set_vexpand(scroll, TRUE);
    gtk_scrolled_window_set_child(GTK_SCROLLED_WINDOW(scroll), app->app_store_view);
    gtk_box_append(GTK_BOX(box), scroll);

    GtkWidget *btn_remove = gtk_button_new_with_label(maku_tr(STR_DEBLOAT_REMOVE));
    gtk_widget_add_css_class(btn_remove, "destructive-action");
    g_signal_connect(btn_remove, "clicked", G_CALLBACK(on_btn_remove_app), app);
    gtk_box_append(GTK_BOX(box), btn_remove);

    maku_refresh_app_list(app);

    return box;
}
