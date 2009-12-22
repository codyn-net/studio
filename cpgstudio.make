

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
	Window.cs \
	Application.cs \
	AssemblyInfo.cs \
	Stock.cs \
	Grid.cs \
	components/Group.cs \
	components/Object.cs \
	components/State.cs \
	components/Link.cs \
	components/Relay.cs \
	components/Simulated.cs \
	components/Network.cs \
	Utils.cs \
	Table.cs \
	PropertyView.cs \
	Allocation.cs \
	components/PropertyAttribute.cs \
	GroupProperties.cs \
	renderers/Renderer.cs \
	renderers/Default.cs \
	renderers/Oscillator.cs \
	renderers/Group.cs \
	renderers/Box.cs \
	renderers/State.cs \
	renderers/Relay.cs \
	Saver.cs \
	serialization/Cpg.cs \
	serialization/Network.cs \
	serialization/Property.cs \
	serialization/State.cs \
	serialization/Simulated.cs \
	serialization/Relay.cs \
	serialization/Link.cs \
	serialization/Project.cs \
	serialization/Object.cs \
	serialization/Group.cs \
	Settings.cs \
	MessageArea.cs \
	PropertyDialog.cs \
	Loader.cs \
	Range.cs \
	Graph.cs \
	Monitor.cs \
	Simulation.cs \
	ObjectView.cs \
	components/Globals.cs \
	serialization/Globals.cs \
	RenderCache.cs \
	FunctionsDialog.cs \
	FunctionsView.cs \
	PolynomialsView.cs \
	interpolators/IInterpolator.cs \
	interpolators/Interpolation.cs \
	interpolators/PChip.cs \
	InterpolateDialog.cs 

DATA_FILES = 

RESOURCES = \
	ui.xml \
	icons/chain.png \
	icons/chain-broken.png \
	icons/link.png \
	monitor-ui.xml 

EXTRAS = \
	cpgstudio.in 

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GLADE_SHARP_20_LIBS) \
	System \
	Mono.Posix \
	System.Drawing \
	Mono.Cairo \
	$(GTK_DOTNET_20_LIBS) \
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
