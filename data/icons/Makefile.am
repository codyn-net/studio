public_icons_themes = \
	hicolor

sizes = 16 22 24 32 48 64 128 192 256

private_icons = $(foreach i,$(sizes),cdn-studio$(i)x$(i).png) cdn-studio.svg

public_icons = \
	$(foreach i,$(sizes),hicolor_apps_$(i)x$(i)_cdn-studio.png) \
	hicolor_apps_scalable_cdn-studio.svg

gtk_update_icon_cache = gtk-update-icon-cache -f -t

iconsdir = $(datadir)/cdn-studio/icons
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
		if [ $$SIZE == "scalable" ]; then \
			SRCFILE=$$ICONFILE; \
		else \
			SRCFILE=$${ICONFILE%.*}$$SIZE.$${ICONFILE#*.}; \
		fi; \
		mkdir -p $(DESTDIR)$(datadir)/icons/$$THEME/$$SIZE/$$CONTEXT; \
		$(INSTALL_DATA) $(srcdir)/$$SRCFILE $(DESTDIR)$(datadir)/icons/$$THEME/$$SIZE/$$CONTEXT/$$ICONFILE; \
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

EXTRA_DIST = $(private_icons)
