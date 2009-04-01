

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG"

ASSEMBLY = bin/Debug/studio.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

CPG_NETWORK_SHARP_1_0_DLL_SOURCE=../cpg-network-sharp/cpg-network/cpg-network-sharp-1.0.dll
STUDIO_DESKTOP_SOURCE=app.desktop

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = bin/Release/studio.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

CPG_NETWORK_SHARP_1_0_DLL_SOURCE=../cpg-network-sharp/cpg-network/cpg-network-sharp-1.0.dll
STUDIO_DESKTOP_SOURCE=app.desktop

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=Cpg.Studio.resources.dll

PROGRAMFILES = \
	$(CPG_NETWORK_SHARP_1_0_DLL)  

LINUX_DESKTOPAPPLICATIONS = \
	$(STUDIO_DESKTOP)  

BINARIES = \
	$(STUDIO)  


	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_DESKTOPAPPLICATIONS) $(BINARIES) 

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
	Allocation.cs \
	components/Relay..cs \
	components/Simulated.cs \
	components/Network.cs 

DATA_FILES = \
	app.desktop 

RESOURCES = \
	ui.xml \
	icons/chain.png \
	icons/chain-broken.png \
	icons/link.png \
	icons/relay.png \
	icons/sensor.png \
	icons/state.png 

EXTRAS = \
	studio.in 

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GLADE_SHARP_20_LIBS) \
	System \
	Mono.Posix

DLL_REFERENCES =  \
	../cpg-network-sharp/cpg-network/cpg-network-sharp-1.0.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_DESKTOPAPPLICATIONS) $(BINARIES) 

include $(top_srcdir)/Makefile.include

STUDIO = $(BUILD_DIR)/studio
CPG_NETWORK_SHARP_1_0_DLL = $(BUILD_DIR)/cpg-network-sharp-1.0.dll
STUDIO_DESKTOP = $(BUILD_DIR)/studio.desktop

$(eval $(call emit-deploy-wrapper,STUDIO,studio,x))
$(eval $(call emit-deploy-target,CPG_NETWORK_SHARP_1_0_DLL))
$(eval $(call emit-deploy-target,STUDIO_DESKTOP))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
