#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include "localization.h"
#include "maku_window.h"
#include "maku_state.h"

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

static char *maku_read_ini_value(const char *path, const char *section, const char *key) {
    FILE *f = fopen(path, "r");
    if (!f) return NULL;

    char *line = NULL;
    size_t cap = 0;
    ssize_t len;
    int in_section = 0;
    char *result = NULL;
    char section_header[128];
    snprintf(section_header, sizeof(section_header), "[%s]", section);

    while ((len = getline(&line, &cap, f)) != -1) {
        size_t line_len = strlen(line);
        while (line_len > 0 && (line[line_len - 1] == '\n' || line[line_len - 1] == '\r')) {
            line[--line_len] = '\0';
        }

        if (line[0] == '[') {
            in_section = (strncmp(line, section_header, strlen(section_header)) == 0);
            continue;
        }

        if (in_section) {
            size_t key_len = strlen(key);
            if (strncmp(line, key, key_len) == 0 && line[key_len] == '=') {
                result = strdup(line + key_len + 1);
                break;
            }
        }
    }
    free(line);
    fclose(f);
    return result;
}

static void maku_replace_palette_position(char *colors_line, int position, const char *hex) {
    char *cursor = colors_line;
    int current = 0;
    char *field_start = cursor;

    while (current < position && *cursor != '\0') {
        if (*cursor == ',') {
            current++;
            cursor++;
            while (*cursor == ' ') cursor++;
            field_start = cursor;
        } else {
            cursor++;
        }
    }

    if (current != position) return;

    char *field_end = field_start;
    while (*field_end != '\0' && *field_end != ',') field_end++;

    size_t old_field_len = (size_t)(field_end - field_start);
    size_t hex_len = strlen(hex);

    size_t prefix_len = (size_t)(field_start - colors_line);
    size_t suffix_len = strlen(field_end);
    size_t total_len = prefix_len + hex_len + suffix_len;

    char *rebuilt = malloc(total_len + 1);
    if (!rebuilt) return;

    memcpy(rebuilt, colors_line, prefix_len);
    memcpy(rebuilt + prefix_len, hex, hex_len);
    memcpy(rebuilt + prefix_len + hex_len, field_end, suffix_len);
    rebuilt[total_len] = '\0';

    memcpy(colors_line, rebuilt, strlen(rebuilt) + 1 < 8192 ? strlen(rebuilt) + 1 : 8191);
    free(rebuilt);

    (void)old_field_len;
}

static void maku_update_qt_color_scheme_file(const char *scheme_path, const char *hex) {
    FILE *f = fopen(scheme_path, "r");
    if (!f) return;

    char *content = NULL;
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
    }
    fclose(f);
    if (!content) return;

    const char *line_prefixes[] = { "active_colors=", "disabled_colors=", "inactive_colors=", NULL };
    char *output = malloc(total + 4096);
    if (!output) {
        free(content);
        return;
    }
    output[0] = '\0';
    size_t output_len = 0;

    char *saveptr = NULL;
    char *line = strtok_r(content, "\n", &saveptr);
    while (line != NULL) {
        int matched = 0;
        for (int i = 0; line_prefixes[i] != NULL; i++) {
            size_t prefix_len = strlen(line_prefixes[i]);
            if (strncmp(line, line_prefixes[i], prefix_len) == 0) {
                char work_buf[8192];
                snprintf(work_buf, sizeof(work_buf), "%s", line + prefix_len);
                maku_replace_palette_position(work_buf, 12, hex);

                size_t needed = strlen(line_prefixes[i]) + strlen(work_buf) + 2;
                if (output_len + needed < total + 4096) {
                    output_len += (size_t)snprintf(output + output_len, total + 4096 - output_len,
                        "%s%s\n", line_prefixes[i], work_buf);
                }
                matched = 1;
                break;
            }
        }
        if (!matched) {
            size_t needed = strlen(line) + 2;
            if (output_len + needed < total + 4096) {
                output_len += (size_t)snprintf(output + output_len, total + 4096 - output_len,
                    "%s\n", line);
            }
        }
        line = strtok_r(NULL, "\n", &saveptr);
    }

    FILE *fw = fopen(scheme_path, "w");
    if (fw) {
        fwrite(output, 1, output_len, fw);
        fclose(fw);
    }

    free(output);
    free(content);
}

static void maku_sync_qt_accent_color(const char *hex) {
    const char *configs[] = {
        ".config/qt5ct/qt5ct.conf",
        ".config/qt6ct/qt6ct.conf",
        NULL
    };

    for (int i = 0; configs[i] != NULL; i++) {
        char conf_path[4096];
        maku_get_config_path(conf_path, sizeof(conf_path), configs[i]);

        char *scheme_path = maku_read_ini_value(conf_path, "Appearance", "color_scheme_path");
        if (!scheme_path) continue;

        maku_update_qt_color_scheme_file(scheme_path, hex);
        free(scheme_path);
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
    maku_sync_qt_accent_color(hex);
    maku_state_set_string(MAKU_STATE_KEY_ACCENT_COLOR, hex);

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
    maku_state_set_bool(MAKU_STATE_KEY_DARK_THEME, state);
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
        "GTK4 + Qt5ct/Qt6ct + Hyprland/Sway", app->btn_pick_color));

    app->sw_dark_theme = gtk_switch_new();
    gtk_switch_set_state(GTK_SWITCH(app->sw_dark_theme), maku_state_get_bool(MAKU_STATE_KEY_DARK_THEME, FALSE));
    gtk_switch_set_active(GTK_SWITCH(app->sw_dark_theme), maku_state_get_bool(MAKU_STATE_KEY_DARK_THEME, FALSE));
    g_signal_connect(app->sw_dark_theme, "state-set", G_CALLBACK(on_toggle_dark_theme), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_PERSONALIZE_DARK),
        "~/.config/gtk-4.0/settings.ini", app->sw_dark_theme));

    return box;
}
