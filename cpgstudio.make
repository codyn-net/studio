

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:3 -optimize+ -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/cpgstudio.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =
BUILD_DIR = bin/Debug

CPGSTUDIO_EXE_MDB_SOURCE=bin/Debug/cpgstudio.exe.mdb
CPGSTUDIO_EXE_MDB=$(BUILD_DIR)/cpgstudio.exe.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = bin/Release/cpgstudio.exe
ASSEMBLY_MDB =
COMPILE_TARGET = exe
PROJECT_REFERENCES =
BUILD_DIR = bin/Release

CPGSTUDIO_EXE_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(CPGSTUDIO_EXE_MDB)

BINARIES = \
	$(CPGSTUDIO)


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES)

FILES = \
	Cpg.Studio/Allocation.cs \
	Cpg.Studio/Application.cs \
	Cpg.Studio/AssemblyInfo.cs \
	Cpg.Studio/Directories.cs \
	Cpg.Studio/DynamicIntegrator.cs \
	Cpg.Studio/FunctionsDialog.cs \
	Cpg.Studio/FunctionsView.cs \
	Cpg.Studio/Graph.cs \
	Cpg.Studio/Grid.cs \
	Cpg.Studio/GroupProperties.cs \
	Cpg.Studio/InterpolateDialog.cs \
	Cpg.Studio/MessageArea.cs \
	Cpg.Studio/Monitor.cs \
	Cpg.Studio/ObjectView.cs \
	Cpg.Studio/PolynomialsView.cs \
	Cpg.Studio/PropertyDialog.cs \
	Cpg.Studio/PropertyView.cs \
	Cpg.Studio/Range.cs \
	Cpg.Studio/RenderCache.cs \
	Cpg.Studio/Settings.cs \
	Cpg.Studio/Simulation.cs \
	Cpg.Studio/Stock.cs \
	Cpg.Studio/Table.cs \
	Cpg.Studio/Utils.cs \
	Cpg.Studio/Window.cs \
	Cpg.Studio.Interpolators/IInterpolator.cs \
	Cpg.Studio.Interpolators/Interpolation.cs \
	Cpg.Studio.Interpolators/PChip.cs \
	Cpg.Studio.Renderers/Box.cs \
	Cpg.Studio.Renderers/Default.cs \
	Cpg.Studio.Renderers/Group.cs \
	Cpg.Studio.Renderers/Oscillator.cs \
	Cpg.Studio.Renderers/Renderer.cs \
	Cpg.Studio.Renderers/State.cs \
	Cpg.Studio.Serialization/Allocation.cs \
	Cpg.Studio.Serialization/Group.cs \
	Cpg.Studio.Serialization/Link.cs \
	Cpg.Studio/Loader.cs \
	Cpg.Studio.Serialization/Main.cs \
	Cpg.Studio.Serialization/Network.cs \
	Cpg.Studio.Serialization/Object.cs \
	Cpg.Studio.Serialization/Project.cs \
	Cpg.Studio.Serialization/Property.cs \
	Cpg.Studio/Saver.cs \
	Cpg.Studio.Serialization/State.cs \
	Cpg.Studio.Wrappers/Graphical.cs \
	Cpg.Studio.Wrappers/Group.cs \
	Cpg.Studio.Wrappers/Link.cs \
	Cpg.Studio.Wrappers/Network.cs \
	Cpg.Studio.Wrappers/State.cs \
	Cpg.Studio.Wrappers/Wrapper.cs

DATA_FILES =

RESOURCES = \
	Cpg.Studio.Resources/ui.xml \
	Cpg.Studio.Resources/chain.png \
	Cpg.Studio.Resources/chain-broken.png \
	Cpg.Studio.Resources/link.png \
	Cpg.Studio.Resources/monitor-ui.xml

EXTRAS = \
	cpgstudio.in

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	System \
	Mono.Posix \
	System.Drawing \
	Mono.Cairo \
	System.Xml \
	$(CPGNETWORK_SHARP_LIBS)

DLL_REFERENCES =

CLEANFILES = $(PROGRAMFILES) $(BINARIES)

include $(top_srcdir)/Makefile.include

CPGSTUDIO = $(BUILD_DIR)/cpgstudio

$(eval $(call emit-deploy-wrapper,CPGSTUDIO,cpgstudio,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

install-data-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		$(INSTALL) -c -m 0755 $$ASM $(DESTDIR)$(pkglibdir); \
	done;

uninstall-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(pkglibdir)/`basename $$ASM`; \
	done;
