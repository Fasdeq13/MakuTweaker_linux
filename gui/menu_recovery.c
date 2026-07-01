#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <sys/stat.h>
#include <unistd.h>
#include <fcntl.h>
#include <zlib.h>
#include <dirent.h>
#include "localization.h"
#include "maku_window.h"
#include "backend_bridge.h"

typedef struct {
    unsigned char name[100];
    unsigned char mode[8];
    unsigned char uid[8];
    unsigned char gid[8];
    unsigned char size[12];
    unsigned char mtime[12];
    unsigned char chksum[8];
    unsigned char typeflag;
    unsigned char linkname[100];
    unsigned char magic[6];
    unsigned char version[2];
    unsigned char uname[32];
    unsigned char gname[32];
    unsigned char devmajor[8];
    unsigned char devminor[8];
    unsigned char prefix[155];
    unsigned char pad[12];
} MakuTarHeader;

static void maku_tar_set_octal(unsigned char *field, size_t fieldlen, unsigned long value) {
    unsigned long max_value = 1UL;
    for (size_t i = 0; i < fieldlen - 1; i++) {
        max_value *= 8UL;
    }
    max_value -= 1UL;
    if (value > max_value) {
        value = max_value;
    }
    snprintf((char *)field, fieldlen, "%0*lo", (int)fieldlen - 1, value);
}

static unsigned int maku_tar_checksum(MakuTarHeader *h) {
    unsigned char *bytes = (unsigned char *)h;
    unsigned int sum = 0;
    for (size_t i = 0; i < sizeof(MakuTarHeader); i++) {
        sum += bytes[i];
    }
    return sum;
}

static void maku_tar_write_header(gzFile gz, const char *relpath, struct stat *st, int is_dir) {
    MakuTarHeader h;
    memset(&h, 0, sizeof(h));

    snprintf((char *)h.name, sizeof(h.name), "%s%s", relpath, is_dir ? "/" : "");
    maku_tar_set_octal(h.mode, sizeof(h.mode), is_dir ? 0755 : (st->st_mode & 0777));
    maku_tar_set_octal(h.uid, sizeof(h.uid), st->st_uid);
    maku_tar_set_octal(h.gid, sizeof(h.gid), st->st_gid);
    maku_tar_set_octal(h.size, sizeof(h.size), is_dir ? 0 : (unsigned long)st->st_size);
    maku_tar_set_octal(h.mtime, sizeof(h.mtime), (unsigned long)st->st_mtime);
    h.typeflag = is_dir ? '5' : '0';
    memcpy(h.magic, "ustar", 5);
    h.magic[5] = '\0';
    h.version[0] = '0';
    h.version[1] = '0';

    memset(h.chksum, ' ', sizeof(h.chksum));
    unsigned int chksum = maku_tar_checksum(&h);
    snprintf((char *)h.chksum, sizeof(h.chksum), "%06o", chksum);
    h.chksum[6] = '\0';
    h.chksum[7] = ' ';

    gzwrite(gz, &h, sizeof(h));
}

static void maku_tar_pad_to_block(gzFile gz, size_t written) {
    size_t remainder = written % 512;
    if (remainder != 0) {
        unsigned char pad[512];
        memset(pad, 0, sizeof(pad));
        gzwrite(gz, pad, 512 - remainder);
    }
}

static void maku_tar_add_directory_recursive(gzFile gz, const char *base_path, const char *rel_prefix) {
    if (strlen(rel_prefix) > 3800) {
        return;
    }

    DIR *d = opendir(base_path);
    if (!d) return;

    struct dirent *entry;
    while ((entry = readdir(d)) != NULL) {
        if (strcmp(entry->d_name, ".") == 0) continue;
        if (strcmp(entry->d_name, "..") == 0) continue;

        char full_path[4608];
        snprintf(full_path, sizeof(full_path), "%s/%s", base_path, entry->d_name);

        char rel_path[4608];
        snprintf(rel_path, sizeof(rel_path), "%s%s", rel_prefix, entry->d_name);

        struct stat st;
        if (lstat(full_path, &st) != 0) continue;

        if (S_ISDIR(st.st_mode)) {
            maku_tar_write_header(gz, rel_path, &st, 1);
            char new_prefix[4608];
            snprintf(new_prefix, sizeof(new_prefix), "%s/", rel_path);
            maku_tar_add_directory_recursive(gz, full_path, new_prefix);
        } else if (S_ISREG(st.st_mode)) {
            maku_tar_write_header(gz, rel_path, &st, 0);
            FILE *f = fopen(full_path, "rb");
            if (f) {
                unsigned char buf[8192];
                size_t n;
                size_t total = 0;
                while ((n = fread(buf, 1, sizeof(buf), f)) > 0) {
                    gzwrite(gz, buf, (unsigned)n);
                    total += n;
                }
                fclose(f);
                maku_tar_pad_to_block(gz, total);
            }
        }
    }
    closedir(d);
}

static void on_btn_backup_configs(GtkButton *btn, gpointer user_data) {
    (void)btn;
    (void)user_data;

    const char *home = getenv("HOME");
    if (!home) return;

    char backup_dir[4096];
    snprintf(backup_dir, sizeof(backup_dir), "%s/.local/share/makutweaker/backups", home);

    char cmd_dir[4096];
    snprintf(cmd_dir, sizeof(cmd_dir), "%s", backup_dir);
    for (char *p = cmd_dir + 1; *p; p++) {
        if (*p == '/') {
            *p = '\0';
            mkdir(cmd_dir, 0755);
            *p = '/';
        }
    }
    mkdir(cmd_dir, 0755);

    time_t now = time(NULL);
    struct tm tm_info;
    localtime_r(&now, &tm_info);
    char timestamp[64];
    strftime(timestamp, sizeof(timestamp), "%Y%m%d_%H%M%S", &tm_info);

    char archive_path[4160];
    snprintf(archive_path, sizeof(archive_path), "%s/config_backup_%s.tar.gz", backup_dir, timestamp);

    gzFile gz = gzopen(archive_path, "wb");
    if (!gz) return;

    char config_path[4096];
    snprintf(config_path, sizeof(config_path), "%s/.config", home);

    maku_tar_add_directory_recursive(gz, config_path, "");

    unsigned char end_blocks[1024];
    memset(end_blocks, 0, sizeof(end_blocks));
    gzwrite(gz, end_blocks, sizeof(end_blocks));

    gzclose(gz);
}

static void on_btn_graphics_reset(GtkButton *btn, gpointer user_data) {
    (void)btn;
    (void)user_data;
    const char *home = getenv("HOME");
    if (!home) return;

    char path[4096];
    snprintf(path, sizeof(path), "%s/.config/gtk-4.0/gtk.css", home);
    unlink(path);
}

static void on_btn_drop_ram_cache(GtkButton *btn, gpointer user_data) {
    (void)btn;
    (void)user_data;
    maku_backend_call("--drop-caches", "3");
}

GtkWidget *maku_build_menu_recovery(MakuAppWidgets *app) {
    GtkWidget *box = gtk_box_new(GTK_ORIENTATION_VERTICAL, 8);
    gtk_widget_set_margin_start(box, 16);
    gtk_widget_set_margin_end(box, 16);
    gtk_widget_set_margin_top(box, 16);
    gtk_widget_set_margin_bottom(box, 16);

    GtkWidget *btn_backup = gtk_button_new_with_label(maku_tr(STR_RECOVERY_BACKUP));
    gtk_widget_add_css_class(btn_backup, "suggested-action");
    g_signal_connect(btn_backup, "clicked", G_CALLBACK(on_btn_backup_configs), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_RECOVERY_BACKUP),
        "~/.config -> ~/.local/share/makutweaker/backups/*.tar.gz", btn_backup));

    GtkWidget *btn_reset = gtk_button_new_with_label(maku_tr(STR_RECOVERY_RESET));
    g_signal_connect(btn_reset, "clicked", G_CALLBACK(on_btn_graphics_reset), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_RECOVERY_RESET),
        "rm ~/.config/gtk-4.0/gtk.css", btn_reset));

    GtkWidget *btn_drop = gtk_button_new_with_label(maku_tr(STR_RECOVERY_DROPCACHE));
    gtk_widget_add_css_class(btn_drop, "destructive-action");
    g_signal_connect(btn_drop, "clicked", G_CALLBACK(on_btn_drop_ram_cache), app);
    gtk_box_append(GTK_BOX(box), maku_make_card(maku_tr(STR_RECOVERY_DROPCACHE),
        "/proc/sys/vm/drop_caches = 3", btn_drop));

    return box;
}
