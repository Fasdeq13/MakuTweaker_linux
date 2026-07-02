#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include "maku_state.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>

static GHashTable *g_state_table = NULL;
static char g_state_path[4096];

static void maku_state_ensure_dir(void) {
    const char *home = getenv("HOME");
    if (!home) home = "/tmp";

    char config_dir[4096];
    snprintf(config_dir, sizeof(config_dir), "%s/.config/makutweaker", home);

    char cmd_dir[4096];
    snprintf(cmd_dir, sizeof(cmd_dir), "%s", config_dir);
    for (char *p = cmd_dir + 1; *p; p++) {
        if (*p == '/') {
            *p = '\0';
            mkdir(cmd_dir, 0755);
            *p = '/';
        }
    }
    mkdir(cmd_dir, 0755);

    snprintf(g_state_path, sizeof(g_state_path), "%s/state.conf", config_dir);
}

static void maku_state_trim_newline(char *s) {
    size_t len = strlen(s);
    while (len > 0 && (s[len - 1] == '\n' || s[len - 1] == '\r')) {
        s[--len] = '\0';
    }
}

void maku_state_load(void) {
    if (g_state_table) {
        g_hash_table_destroy(g_state_table);
    }
    g_state_table = g_hash_table_new_full(g_str_hash, g_str_equal, g_free, g_free);

    maku_state_ensure_dir();

    FILE *f = fopen(g_state_path, "r");
    if (!f) {
        return;
    }

    char *line = NULL;
    size_t cap = 0;
    ssize_t len;
    while ((len = getline(&line, &cap, f)) != -1) {
        maku_state_trim_newline(line);
        if (line[0] == '\0' || line[0] == '#') continue;

        char *eq = strchr(line, '=');
        if (!eq) continue;

        *eq = '\0';
        const char *key = line;
        const char *value = eq + 1;

        g_hash_table_insert(g_state_table, g_strdup(key), g_strdup(value));
    }
    free(line);
    fclose(f);
}

void maku_state_save(void) {
    if (!g_state_table) return;
    if (g_state_path[0] == '\0') {
        maku_state_ensure_dir();
    }

    FILE *f = fopen(g_state_path, "w");
    if (!f) return;

    GHashTableIter iter;
    gpointer key, value;
    g_hash_table_iter_init(&iter, g_state_table);
    while (g_hash_table_iter_next(&iter, &key, &value)) {
        fprintf(f, "%s=%s\n", (const char *)key, (const char *)value);
    }

    fclose(f);
}

gboolean maku_state_get_bool(const char *key, gboolean default_value) {
    if (!g_state_table) return default_value;
    const char *value = g_hash_table_lookup(g_state_table, key);
    if (!value) return default_value;
    return strcmp(value, "1") == 0;
}

void maku_state_set_bool(const char *key, gboolean value) {
    if (!g_state_table) return;
    g_hash_table_insert(g_state_table, g_strdup(key), g_strdup(value ? "1" : "0"));
    maku_state_save();
}

const char *maku_state_get_string(const char *key, const char *default_value) {
    if (!g_state_table) return default_value;
    const char *value = g_hash_table_lookup(g_state_table, key);
    return value ? value : default_value;
}

void maku_state_set_string(const char *key, const char *value) {
    if (!g_state_table) return;
    g_hash_table_insert(g_state_table, g_strdup(key), g_strdup(value));
    maku_state_save();
}
