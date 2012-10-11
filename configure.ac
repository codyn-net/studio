dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT(cdn-studio, 3.0)
AM_INIT_AUTOMAKE([1.11 tar-ustar dist-xz no-dist-gzip -Wno-portability])
AM_MAINTAINER_MODE([enable])
AC_CONFIG_MACRO_DIR([m4])
dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

dnl check for dmcs first
AC_PATH_PROGS(CMCS, dmcs gmcs, no)

if test "x$CMCS" = "xno"; then
	AC_MSG_ERROR([dmcs and gmcs not found])
fi

AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=YES]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE, true)
	enable_release=yes
fi


dnl package checks, common for all configs
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])

dnl package checks, per config
PKG_CHECK_MODULES([CDNNETWORK_SHARP], [codyn-sharp-3.0])

PKG_CHECK_MODULES([BIOROB_MATH_SHARP], [biorob-math-sharp-1.0])
PKG_CHECK_MODULES([PLOT_SHARP], [plot-sharp-1])

dnl find assembly paths for unstable installed assemblies that will be
dnl copied into monoskel installation directory for local usage with the app
UNSTABLE_INSTALL_ASSEMBLIES="codyn-sharp-3.0 biorob-math-sharp-1.0 plot-sharp-1"
INSTALLED_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries \"$UNSTABLE_INSTALL_ASSEMBLIES\"`"

AC_SUBST(INSTALLED_ASSEMBLIES)

AC_CONFIG_FILES([
data/Makefile
data/icons/Makefile
m4/Makefile
cdn-studio
Makefile
Cdn.Studio/Config.cs
])

AC_OUTPUT
