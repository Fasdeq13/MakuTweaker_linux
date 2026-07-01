CC := gcc
STD := -std=c11
WARN := -Wall -Wextra
OPT := -O2

PKG_CONFIG := pkg-config
GTK_CFLAGS := $(shell $(PKG_CONFIG) --cflags gtk4)
GTK_LIBS := $(shell $(PKG_CONFIG) --libs gtk4)

INCLUDES := -Igui -Icommon
CFLAGS := $(STD) $(WARN) $(OPT) $(GTK_CFLAGS) $(INCLUDES) -pthread
LDFLAGS := $(GTK_LIBS) -pthread -lz

GUI_SRCS := \
	gui/main.c \
	gui/localization.c \
	gui/widgets_common.c \
	gui/backend_bridge.c \
	gui/menu_info.c \
	gui/menu_perf.c \
	gui/menu_components.c \
	gui/menu_processes.c \
	gui/menu_personalize.c \
	gui/menu_debloat.c \
	gui/menu_cleanup.c \
	gui/menu_security.c \
	gui/menu_filemgr.c \
	gui/menu_recovery.c

COMMON_SRCS := \
	common/maku_common.c

BACKEND_SRCS := \
	backend/tweaker_backend.c \
	common/maku_common.c

GUI_BIN := makutweaker_gui
BACKEND_BIN := tweaker_backend

# Опциональный модуль видеообоев через wlr-layer-shell-unstable-v1.
# Требует сгенерированного протокола (wayland-scanner) — не входит в
# основную сборку, чтобы `make` не падал при отсутствии заголовка.
# Как включить:
#   1) wayland-scanner client-header \
#        /usr/share/wlr-protocols/unstable/wlr-layer-shell-unstable-v1.xml \
#        gui/wlr-layer-shell-unstable-v1-client-protocol.h
#      wayland-scanner private-code \
#        /usr/share/wlr-protocols/unstable/wlr-layer-shell-unstable-v1.xml \
#        gui/wlr-layer-shell-unstable-v1-protocol.c
#   2) make wallpaper
WALLPAPER_SRCS := \
	gui/menu_wallpaper_layershell.c \
	gui/wlr-layer-shell-unstable-v1-protocol.c
WALLPAPER_CFLAGS := $(shell $(PKG_CONFIG) --cflags wayland-client)
WALLPAPER_LIBS := $(shell $(PKG_CONFIG) --libs wayland-client)

.PHONY: all clean install gui backend wallpaper

all: gui backend

gui: $(GUI_BIN)

backend: $(BACKEND_BIN)

$(GUI_BIN): $(GUI_SRCS) $(COMMON_SRCS)
	$(CC) $(CFLAGS) $(GUI_SRCS) $(COMMON_SRCS) -o $@ $(LDFLAGS)

$(BACKEND_BIN): $(BACKEND_SRCS)
	$(CC) $(STD) $(WARN) $(OPT) -Icommon $(BACKEND_SRCS) -o $@

wallpaper: $(WALLPAPER_SRCS)
	$(CC) $(CFLAGS) $(WALLPAPER_CFLAGS) -c gui/menu_wallpaper_layershell.c -o gui/menu_wallpaper_layershell.o
	@echo "layer-shell object built: gui/menu_wallpaper_layershell.o"
	@echo "link it into $(GUI_BIN) manually with: $(WALLPAPER_LIBS)"

clean:
	rm -f $(GUI_BIN) $(BACKEND_BIN) gui/*.o

install: all
	install -Dm755 $(GUI_BIN) /usr/local/bin/$(GUI_BIN)
	install -Dm755 $(BACKEND_BIN) /usr/local/bin/$(BACKEND_BIN)
	install -Dm644 styles/style.css /usr/local/share/makutweaker/style.css
