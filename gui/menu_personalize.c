#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include "localization.h"
#include "maku_window.h"

static void maku_get_config_path(char *buf, size_t buflen, const char *rel) {
    const char *home = getenv("HOME");
    if (!home) home = "/tmp";
    snprintf(buf, buflen, "%s/%s", home, rel);
}

static void maku_write_gtk_css_colors(const char *hex) {
    char path[4096];
    maku_get_config_path(path, sizeof(path), ".config/gtk-4.0");

    char cmd_dir[4096];
    snprintf(cmd_dir, sizeof(cmd_dir), "%s", path);
    for (char *p = cmd_dir + 1; *p; p++) {
        if (*p == '/') {
            *p = '\0';
            mkdir(cmd_dir, 0755);
            *p = '/';
        }
    }
    mkdir(cmd_dir, 0755);

    char css_path[4160];
    snprintf(css_path, sizeof(css_path), "%s/gtk.css", path);
    FILE *f = fopen(css_path, "w");
    if (!f) return;
    fprintf(f, "@define-color accent_color %s;\n", hex);
    fprintf(f, "@define-color accent_bg_color %s;\n", hex);
    fclose(f);
}

static void maku_sync_wm_border_color(const char *hex) {
    const char *files[] = {
        ".config/hypr/hyprland.conf",
        ".config/sway/config",
        NULL
    };
    for (int i = 0; files[i] != NULL; i++) {
        char path[4096];
        maku_get_config_path(path, sizeof(path), files[i]);
        FILE *f = fopen(path, "r");
        if (!f) continue;

        char *content = NULL;
        size_t cap = 0;
        size_t total = 0;
        char chunk[4096];
        size_t n;
        while ((n = fread(chunk, 1, sizeof(chunk), f)) > 0) {
            char *tmp = realloc(content, total + n + 1);
            if (!tmp) break;
            content = tmp;
            memcpy(content + total, chunk, n);
            total += n;
            content[total] = '\0';
            cap = total + 1;
        }
        fclose(f);
        (void)cap;
        if (!content) continue;

        char *pos = strstr(content, "col.active_border");
        if (pos) {
            char *line_end = strchr(pos, '\n');
            size_t line_len = line_end ? (size_t)(line_end - pos) : strlen(pos);

            char new_line[256];
            snprintf(new_line, sizeof(new_line), "col.active_border %s", hex);

            size_t new_len = strlen(new_line);
            size_t rest_len = total - (size_t)(pos - content) - line_len;
            size_t prefix_len = (size_t)(pos - content);

            char *rebuilt = malloc(prefix_len + new_len + rest_len + 1);
            if (rebuilt) {
                memcpy(rebuilt, content, prefix_len);
                memcpy(rebuilt + prefix_len, new_line, new_len);
                memcpy(rebuilt + prefix_len + new_len, pos + line_len, rest_len);
                rebuilt[prefix_len + new_len + rest_len] = '\0';

                FILE *fw = fopen(path, "w");
                if (fw) {
                    fwrite(rebuilt, 1, strlen(rebuilt), fw);
                    fclose(fw);
                }
                free(rebuilt);
            }
        }
        free(content);
    }
}

static void on_color_chosen(GObject *source, GAsyncResult *res, gpointer user_data) {
    (void)user_data;
    GtkColorDialog *dialog = GTK_COLOR_DIALOG(source);
    GdkRGBA *rgba = gtk_color_dialog_choose_rgba_finish(dialog, res, NULL);
    if (!rgba) return;

    int r = (int)(rgba->red * 255.0f);
    int g = (int)(rgba->green * 255.0f);
    int b = (int)(rgba->blue * 255.0f);

    char hex[8];
    snprintf(hex, sizeof(hex), "#%02x%02x%02x", r, g, b);

    maku_write_gtk_css_colors(hex);
    maku_sync_wm_border_color(hex);

    gdk_rgba_free(rgba);
}

static void on_btn_pick_color(GtkButton *btn, gpointer user_data) {
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;
    GtkColorDialog *dialog = gtk_color_dialog_new();
    gtk_color_dialog_choose_rgba(dialog, GTK_WINDOW(app->window), NULL, NULL, on_color_chosen, app);
    g_object_unref(dialog);
    (void)btn;
}

static void on_toggle_dark_theme(GtkSwitch *sw, gboolean state, gpointer user_data) {
    (void)user_data;
    char path[4096];
    maku_get_config_path(path, sizeof(path), ".config/gtk-4.0");

    char cmd_dir[4096];
    snprintf(cmd_dir, sizeof(cmd_dir), "%s", path);
    for (char *p = cmd_dir + 1; *p; p++) {
        if (*p == '/') {
            *p = '\0';
            mkdir(cmd_dir, 0755);
            *p = '/';
        }
    }
    mkdir(cmd_dir, 0755);

    char settings_path[4160];
    snprintf(settings_path, sizeof(settings_path), "%s/settings.ini", path);
    FILE *f = fopen(settings_path, "w");
    if (f) {
        fprintf(f, "[Settings]\n");
        fprintf(f, "gtk-application-prefer-dark-theme=%d\n", state ? 1 : 0);
        fclose(f);
    }
    gtk_switch_set_state(sw, state);
}

GtkWidget *maku_build_menu_personalize(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    app->btn_pick_color = gtk_button_new_with_label(maku_tr(STR_PERSONALIZE_COLOR));
    gtk_widget_add_css_class(app->btn_pick_color, "suggested-action");
    g_signal_connect(app->btn_pick_color, "clicked", G_CALLBACK(on_btn_pick_color), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_PERSONALIZE_COLOR),
        "GtkColorDialog -> ~/.config/gtk-4.0/gtk.css + Hyprland/Sway", app->btn_pick_color));

    app->sw_dark_theme = gtk_switch_new();
    g_signal_connect(app->sw_dark_theme, "state-set", G_CALLBACK(on_toggle_dark_theme), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_PERSONALIZE_DARK),
        "~/.config/gtk-4.0/settings.ini", app->sw_dark_theme));

    return box;
}
