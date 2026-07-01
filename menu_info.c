#include <gtk/gtk.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include "localization.h"
#include "maku_window.h"
#include "maku_common.h"

static char *maku_read_os_release_field(const char *field) {
    FILE *f = fopen(MAKU_OS_RELEASE, "r");
    if (!f) return strdup("Unknown");
    char *line = NULL;
    size_t cap = 0;
    ssize_t len;
    char *result = NULL;
    size_t field_len = strlen(field);
    while ((len = getline(&line, &cap, f)) != -1) {
        if (strncmp(line, field, field_len) == 0) {
            char *val = line + field_len;
            size_t vl = strlen(val);
            while (vl > 0 && (val[vl - 1] == '\n' || val[vl - 1] == '\r')) {
                val[--vl] = '\0';
            }
            if (vl > 0 && val[0] == '"') {
                val++;
                vl--;
                if (vl > 0 && val[vl - 1] == '"') {
                    val[vl - 1] = '\0';
                }
            }
            result = strdup(val);
            break;
        }
    }
    free(line);
    fclose(f);
    if (!result) result = strdup("Unknown");
    return result;
}

static char *maku_read_cpu_model_name(void) {
    FILE *f = fopen(MAKU_PROC_CPUINFO, "r");
    if (!f) return strdup("Unknown CPU");
    char *line = NULL;
    size_t cap = 0;
    ssize_t len;
    char *result = NULL;
    while ((len = getline(&line, &cap, f)) != -1) {
        if (strncmp(line, "model name", 10) == 0) {
            char *colon = strchr(line, ':');
            if (colon) {
                colon++;
                while (*colon == ' ' || *colon == '\t') colon++;
                size_t vl = strlen(colon);
                while (vl > 0 && (colon[vl - 1] == '\n' || colon[vl - 1] == '\r')) {
                    colon[--vl] = '\0';
                }
                result = strdup(colon);
            }
            break;
        }
    }
    free(line);
    fclose(f);
    if (!result) result = strdup("Unknown CPU");
    return result;
}

static char *maku_read_cpu_frequencies(void) {
    long cpu_count = maku_get_online_cpu_count();
    char *buffer = malloc(4096);
    buffer[0] = '\0';
    size_t used = 0;
    for (long i = 0; i < cpu_count; i++) {
        char path[256];
        snprintf(path, sizeof(path), MAKU_CPU_CURFREQ_FMT, (int)i);
        FILE *f = fopen(path, "r");
        long khz = 0;
        if (f) {
            if (fscanf(f, "%ld", &khz) != 1) khz = 0;
            fclose(f);
        }
        char part[64];
        double mhz = khz / 1000.0;
        int n = snprintf(part, sizeof(part), "CPU%ld: %.0f MHz  ", i, mhz);
        if (used + (size_t)n < 4096) {
            memcpy(buffer + used, part, (size_t)n);
            used += (size_t)n;
            buffer[used] = '\0';
        }
    }
    return buffer;
}

static void maku_read_meminfo(unsigned long *total_kb, unsigned long *avail_kb) {
    *total_kb = 0;
    *avail_kb = 0;
    FILE *f = fopen(MAKU_PROC_MEMINFO, "r");
    if (!f) return;
    char *line = NULL;
    size_t cap = 0;
    ssize_t len;
    int got_total = 0, got_avail = 0;
    while ((len = getline(&line, &cap, f)) != -1 && !(got_total && got_avail)) {
        unsigned long val;
        if (sscanf(line, "MemTotal: %lu kB", &val) == 1) {
            *total_kb = val;
            got_total = 1;
        } else if (sscanf(line, "MemAvailable: %lu kB", &val) == 1) {
            *avail_kb = val;
            got_avail = 1;
        }
    }
    free(line);
    fclose(f);
}

static char *maku_format_uptime(void) {
    FILE *f = fopen(MAKU_PROC_UPTIME, "r");
    double uptime_sec = 0.0;
    if (f) {
        if (fscanf(f, "%lf", &uptime_sec) != 1) uptime_sec = 0.0;
        fclose(f);
    }
    long total = (long)uptime_sec;
    long hours = total / 3600;
    long minutes = (total % 3600) / 60;
    long seconds = total % 60;
    char *buf = malloc(64);
    snprintf(buf, 64, "%02ld:%02ld:%02ld", hours, minutes, seconds);
    return buf;
}

gboolean maku_info_timer_cb(gpointer user_data) {
    MakuAppWidgets *app = (MakuAppWidgets *)user_data;

    if (app->lbl_cpu_freq) {
        char *freqs = maku_read_cpu_frequencies();
        gtk_label_set_text(GTK_LABEL(app->lbl_cpu_freq), freqs);
        free(freqs);
    }

    if (app->bar_ram && app->lbl_ram_text) {
        unsigned long total_kb, avail_kb;
        maku_read_meminfo(&total_kb, &avail_kb);
        unsigned long used_kb = (total_kb > avail_kb) ? (total_kb - avail_kb) : 0;
        double fraction = total_kb > 0 ? ((double)used_kb / (double)total_kb) : 0.0;
        gtk_progress_bar_set_fraction(GTK_PROGRESS_BAR(app->bar_ram), fraction);
        char text[128];
        snprintf(text, sizeof(text), "%.1f GB / %.1f GB",
                 used_kb / 1048576.0, total_kb / 1048576.0);
        gtk_label_set_text(GTK_LABEL(app->lbl_ram_text), text);
    }

    if (app->lbl_uptime) {
        char *up = maku_format_uptime();
        gtk_label_set_text(GTK_LABEL(app->lbl_uptime), up);
        free(up);
    }

    return G_SOURCE_CONTINUE;
}

GtkWidget *maku_build_menu_info(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    char *distro_name = maku_read_os_release_field("NAME=");
    char *distro_ver = maku_read_os_release_field("VERSION_ID=");
    char distro_full[256];
    snprintf(distro_full, sizeof(distro_full), "%s %s", distro_name, distro_ver);
    free(distro_name);
    free(distro_ver);

    GtkWidget *card_distro = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
    gtk_widget_add_css_class(card_distro, "maku-card");
    GtkWidget *lbl_distro_title = gtk_label_new(maku_tr(STR_DISTRO_LABEL));
    gtk_widget_add_css_class(lbl_distro_title, "maku-card-title");
    app->lbl_distro = gtk_label_new(distro_full);
    gtk_box_append(GTK_BOX(card_distro), lbl_distro_title);
    gtk_box_append(GTK_BOX(card_distro), app->lbl_distro);
    gtk_box_append(GTK_BOX(box), card_distro);

    const char *session_type = getenv("XDG_SESSION_TYPE");
    GtkWidget *card_session = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
    gtk_widget_add_css_class(card_session, "maku-card");
    GtkWidget *lbl_session_title = gtk_label_new(maku_tr(STR_SESSION_LABEL));
    gtk_widget_add_css_class(lbl_session_title, "maku-card-title");
    gtk_box_append(GTK_BOX(card_session), lbl_session_title);
    if (session_type && strcmp(session_type, "wayland") == 0) {
        app->lbl_session = gtk_label_new(session_type);
        gtk_widget_add_css_class(app->lbl_session, "maku-accent");
    } else {
        app->lbl_session = gtk_label_new(maku_tr(STR_SESSION_UNSUPPORTED));
        gtk_widget_add_css_class(app->lbl_session, "maku-warning");
    }
    gtk_box_append(GTK_BOX(card_session), app->lbl_session);
    gtk_box_append(GTK_BOX(box), card_session);

    char *cpu_name = maku_read_cpu_model_name();
    GtkWidget *card_cpu = gtk_box_new(GTK_ORIENTATION_VERTICAL, 4);
    gtk_widget_add_css_class(card_cpu, "maku-card");
    GtkWidget *lbl_cpu_title = gtk_label_new(maku_tr(STR_CPU_LABEL));
    gtk_widget_add_css_class(lbl_cpu_title, "maku-card-title");
    app->lbl_cpu_name = gtk_label_new(cpu_name);
    gtk_label_set_xalign(GTK_LABEL(app->lbl_cpu_name), 0.0f);
    app->lbl_cpu_freq = gtk_label_new("");
    gtk_label_set_xalign(GTK_LABEL(app->lbl_cpu_freq), 0.0f);
    gtk_widget_add_css_class(app->lbl_cpu_freq, "maku-card-subtitle");
    gtk_box_append(GTK_BOX(card_cpu), lbl_cpu_title);
    gtk_box_append(GTK_BOX(card_cpu), app->lbl_cpu_name);
    gtk_box_append(GTK_BOX(card_cpu), app->lbl_cpu_freq);
    gtk_box_append(GTK_BOX(box), card_cpu);
    free(cpu_name);

    GtkWidget *card_ram = gtk_box_new(GTK_ORIENTATION_VERTICAL, 4);
    gtk_widget_add_css_class(card_ram, "maku-card");
    GtkWidget *lbl_ram_title = gtk_label_new(maku_tr(STR_RAM_LABEL));
    gtk_widget_add_css_class(lbl_ram_title, "maku-card-title");
    app->bar_ram = gtk_progress_bar_new();
    gtk_widget_set_hexpand(app->bar_ram, TRUE);
    app->lbl_ram_text = gtk_label_new("");
    gtk_box_append(GTK_BOX(card_ram), lbl_ram_title);
    gtk_box_append(GTK_BOX(card_ram), app->bar_ram);
    gtk_box_append(GTK_BOX(card_ram), app->lbl_ram_text);
    gtk_box_append(GTK_BOX(box), card_ram);

    GtkWidget *card_uptime = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 8);
    gtk_widget_add_css_class(card_uptime, "maku-card");
    GtkWidget *lbl_uptime_title = gtk_label_new(maku_tr(STR_UPTIME_LABEL));
    gtk_widget_add_css_class(lbl_uptime_title, "maku-card-title");
    app->lbl_uptime = gtk_label_new("00:00:00");
    gtk_box_append(GTK_BOX(card_uptime), lbl_uptime_title);
    gtk_box_append(GTK_BOX(card_uptime), app->lbl_uptime);
    gtk_box_append(GTK_BOX(box), card_uptime);

    maku_info_timer_cb(app);

    return box;
}
