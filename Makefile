CC := gcc
STD := -std=c11
WARN := -Wall -Wextra
OPT := -O2

PKG_CONFIG := pkg-config
GTK_CFLAGS := $(shell $(PKG_CONFIG) --cflags gtk4)
GTK_LIBS := $(shell $(PKG_CONFIG) --libs gtk4)

CFLAGS := $(STD) $(WARN) $(OPT) $(GTK_CFLAGS) -I. -pthread
LDFLAGS := $(GTK_LIBS) -pthread -lz

GUI_SRCS := \
	main.c \
	localization.c \
	widgets_common.c \
	backend_bridge.c \
	maku_state.c \
	maku_common.c \
	menu_info.c \
	menu_perf.c \
	menu_components.c \
	menu_processes.c \
	menu_personalize.c \
	menu_debloat.c \
	menu_cleanup.c \
	menu_security.c \
	menu_filemgr.c \
	menu_recovery.c \
	menu_gnome.c

BACKEND_SRCS := \
	tweaker_backend.c \
	maku_common.c

GUI_BIN := makutweaker_gui
BACKEND_BIN := tweaker_backend

# Опциональный модуль видеообоев через wlr-layer-shell-unstable-v1.
# Требует сгенерированного протокола (wayland-scanner) — не входит в
# основную сборку, чтобы `make` не падал при отсутствии заголовка.
# Как включить:
#   1) wayland-scanner client-header \
#        /usr/share/wlr-protocols/unstable/wlr-layer-shell-unstable-v1.xml \
#        wlr-layer-shell-unstable-v1-client-protocol.h
#      wayland-scanner private-code \
#        /usr/share/wlr-protocols/unstable/wlr-layer-shell-unstable-v1.xml \
#        wlr-layer-shell-unstable-v1-protocol.c
#   2) make wallpaper эт по желанию
WALLPAPER_SRCS := \
	menu_wallpaper_layershell.c \
	wlr-layer-shell-unstable-v1-protocol.c
WALLPAPER_CFLAGS := $(shell $(PKG_CONFIG) --cflags wayland-client)
WALLPAPER_LIBS := $(shell $(PKG_CONFIG) --libs wayland-client)

.PHONY: all clean install gui backend wallpaper

all: gui backend

gui: $(GUI_BIN)

backend: $(BACKEND_BIN)

$(GUI_BIN): $(GUI_SRCS)
	$(CC) $(CFLAGS) $(GUI_SRCS) -o $@ $(LDFLAGS)

$(BACKEND_BIN): $(BACKEND_SRCS)
	$(CC) $(STD) $(WARN) $(OPT) -I. $(BACKEND_SRCS) -o $@

wallpaper: $(WALLPAPER_SRCS)
	$(CC) $(CFLAGS) $(WALLPAPER_CFLAGS) -c menu_wallpaper_layershell.c -o menu_wallpaper_layershell.o
	@echo "layer-shell object built: menu_wallpaper_layershell.o"
	@echo "link it into $(GUI_BIN) manually with: $(WALLPAPER_LIBS)"

clean:
	rm -f $(GUI_BIN) $(BACKEND_BIN) *.o

install: all
	install -Dm755 $(GUI_BIN) /usr/local/bin/$(GUI_BIN)
	install -Dm755 $(BACKEND_BIN) /usr/local/bin/$(BACKEND_BIN)
	install -Dm644 style.css /usr/local/share/makutweaker/style.css
