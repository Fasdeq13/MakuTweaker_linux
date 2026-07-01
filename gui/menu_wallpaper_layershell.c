#define _DEFAULT_SOURCE
#define _XOPEN_SOURCE 700

#include <gtk/gtk.h>
#include <gdk/wayland/gdkwayland.h>
#include <wayland-client.h>
#include "wlr-layer-shell-unstable-v1-client-protocol.h"

static struct zwlr_layer_shell_v1 *g_layer_shell = NULL;

static void registry_global(void *data, struct wl_registry *registry,
                             uint32_t name, const char *interface, uint32_t version) {
    (void)data;
    (void)version;
    if (strcmp(interface, zwlr_layer_shell_v1_interface.name) == 0) {
        g_layer_shell = wl_registry_bind(registry, name, &zwlr_layer_shell_v1_interface, 1);
    }
}

static void registry_global_remove(void *data, struct wl_registry *registry, uint32_t name) {
    (void)data;
    (void)registry;
    (void)name;
}

static const struct wl_registry_listener registry_listener = {
    .global = registry_global,
    .global_remove = registry_global_remove,
};

GtkWidget *maku_create_wallpaper_window(GtkApplication *gtk_app) {
    GtkWidget *win = gtk_application_window_new(gtk_app);
    gtk_widget_realize(win);

    GdkSurface *surface = gtk_native_get_surface(GTK_NATIVE(win));
    if (!GDK_IS_WAYLAND_SURFACE(surface)) {
        return win;
    }

    struct wl_display *display = gdk_wayland_display_get_wl_display(gdk_surface_get_display(surface));
    struct wl_registry *registry = wl_display_get_registry(display);
    wl_registry_add_listener(registry, &registry_listener, NULL);
    wl_display_roundtrip(display);

    if (!g_layer_shell) {
        return win;
    }

    struct wl_surface *wl_surf = gdk_wayland_surface_get_wl_surface(surface);
    struct zwlr_layer_surface_v1 *layer_surface = zwlr_layer_shell_v1_get_layer_surface(
        g_layer_shell, wl_surf, NULL,
        ZWLR_LAYER_SHELL_V1_LAYER_BACKGROUND, "makutweaker-wallpaper"
    );

    zwlr_layer_surface_v1_set_anchor(layer_surface,
        ZWLR_LAYER_SURFACE_V1_ANCHOR_TOP | ZWLR_LAYER_SURFACE_V1_ANCHOR_BOTTOM |
        ZWLR_LAYER_SURFACE_V1_ANCHOR_LEFT | ZWLR_LAYER_SURFACE_V1_ANCHOR_RIGHT);
    zwlr_layer_surface_v1_set_exclusive_zone(layer_surface, -1);
    wl_surface_commit(wl_surf);

    return win;
}
