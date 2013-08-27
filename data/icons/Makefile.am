public_icons_themes = \
	hicolor	\
	HighContrastLargePrint \
	HighContrastLargePrintInverse \
	LowContrastLargePrint

public_icons = \
	hicolor_apps_16x16_cdnstudio.png \
	hicolor_apps_22x22_cdnstudio.png \
	hicolor_apps_24x24_cdnstudio.png \
	hicolor_apps_32x32_cdnstudio.png \
	hicolor_apps_48x48_cdnstudio.png \
	hicolor_apps_64x64_cdnstudio.png \
	hicolor_apps_128x128_cdnstudio.png \
	hicolor_apps_192x192_cdnstudio.png \
	hicolor_apps_256x256_cdnstudio.png \
	hicolor_apps_scalable_cdnstudio.svg

private_icons = \
	cdnstudio16x16.png	\
	cdnstudio22x22.png	\
	cdnstudio24x24.png	\
	cdnstudio32x32.png	\
	cdnstudio48x48.png	\
	cdnstudio64x64.png	\
	cdnstudio128x128.png \
	cdnstudio192x192.png \
	cdnstudio256x256.png \
	cdnstudio.svg

gtk_update_icon_cache = gtk-update-icon-cache -f -t

iconsdir = $(datadir)/cdnstudio/icons
icons_DATA = $(private_icons)

update-icon-cache:
	@-if test -z "$(DESTDIR)"; then \
		echo "Updating Gtk icon cache."; \
		for theme in $(public_icons_themes); do \
			$(gtk_update_icon_cache) $(datadir)/icons/$$theme; \
		done; \
	else \
		echo "*** Icon cache not updated.  After (un)install, run this:"; \
		for theme in $(public_icons_themes); do \
			echo "***   $(gtk_update_icon_cache) $(datadir)/icons/$$theme"; \
		done; \
	fi

install-icons:
	for icon in $(public_icons); do \
		THEME=`echo $$icon | cut -d_ -f1`; \
		CONTEXT=`echo $$icon | cut -d_ -f2`; \
		SIZE=`echo $$icon | cut -d_ -f3`; \
		ICONFILE=`echo $$icon | cut -d_ -f4`; \
		mkdir -p $(DESTDIR)$(datadir)/icons/$$THEME/$$SIZE/$$CONTEXT; \
		$(INSTALL_DATA) $(srcdir)/$$icon $(DESTDIR)$(datadir)/icons/$$THEME/$$SIZE/$$CONTEXT/$$ICONFILE; \
	done;

uninstall-icons:
	for icon in $(public_icons); do \
		THEME=`echo $$icon | cut -d_ -f1`; \
		CONTEXT=`echo $$icon | cut -d_ -f2`; \
		SIZE=`echo $$icon | cut -d_ -f3`; \
		ICONFILE=`echo $$icon | cut -d_ -f4`; \
		rm -f $(DESTDIR)$(datadir)/icons/$$THEME/$$SIZE/$$CONTEXT/$$ICONFILE; \
	done;

install-data-local: install-icons
install-data-hook: update-icon-cache

uninstall-local: uninstall-icons
uninstall-hook: update-icon-cache

EXTRA_DIST = 				\
	$(public_icons) \
	$(private_icons)
