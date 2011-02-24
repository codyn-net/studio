

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
	Cpg.Studio/Actions.cs \
	Cpg.Studio/Allocation.cs \
	Cpg.Studio/Anchor.cs \
	Cpg.Studio/Application.cs \
	Cpg.Studio/AssemblyInfo.cs \
	Cpg.Studio.Clipboard/Internal.cs \
	Cpg.Studio.Dialogs/Functions.cs \
	Cpg.Studio.Dialogs/Interpolate.cs \
	Cpg.Studio.Dialogs/Property.cs \
	Cpg.Studio.Dialogs/Import.cs \
	Cpg.Studio/Directories.cs \
	Cpg.Studio/DynamicIntegrator.cs \
	Cpg.Studio.Interpolators/IInterpolator.cs \
	Cpg.Studio.Interpolators/Interpolation.cs \
	Cpg.Studio.Interpolators/PChip.cs \
	Cpg.Studio/Point.cs \
	Cpg.Studio/Range.cs \
	Cpg.Studio/RenderCache.cs \
	Cpg.Studio.Renderers/Box.cs \
	Cpg.Studio.Renderers/Group.cs \
	Cpg.Studio.Renderers/Link.cs \
	Cpg.Studio.Renderers/Oscillator.cs \
	Cpg.Studio.Renderers/Renderer.cs \
	Cpg.Studio.Renderers/State.cs \
	Cpg.Studio.Renderers/Input.cs \
	Cpg.Studio.Serialization/FunctionPolynomial.cs \
	Cpg.Studio.Serialization/Functions.cs \
	Cpg.Studio.Serialization/Group.cs \
	Cpg.Studio.Serialization/Link.cs \
	Cpg.Studio.Serialization/InputFile.cs \
	Cpg.Studio.Serialization/Network.cs \
	Cpg.Studio.Serialization/Object.cs \
	Cpg.Studio.Serialization/Project.cs \
	Cpg.Studio.Serialization/State.cs \
	Cpg.Studio.Serialization/Templates.cs \
	Cpg.Studio/Settings.cs \
	Cpg.Studio/Simulation.cs \
	Cpg.Studio/Stock.cs \
	Cpg.Studio.Undo/AddFunctionPolynomialPiece.cs \
	Cpg.Studio.Undo/AddGroup.cs \
	Cpg.Studio.Undo/AddInterfaceProperty.cs \
	Cpg.Studio.Undo/AddLinkAction.cs \
	Cpg.Studio.Undo/AddObject.cs \
	Cpg.Studio.Undo/AddProperty.cs \
	Cpg.Studio.Undo/ApplyTemplate.cs \
	Cpg.Studio.Undo/AttachLink.cs \
	Cpg.Studio.Undo/Function.cs \
	Cpg.Studio.Undo/FunctionPolynomialPiece.cs \
	Cpg.Studio.Undo/Group.cs \
	Cpg.Studio.Undo/IAction.cs \
	Cpg.Studio.Undo/Import.cs \
	Cpg.Studio.Undo/InterfaceProperty.cs \
	Cpg.Studio.Undo/LinkAction.cs \
	Cpg.Studio.Undo/Manager.cs \
	Cpg.Studio.Undo/ModifyExpression.cs \
	Cpg.Studio.Undo/ModifyFunctionArguments.cs \
	Cpg.Studio.Undo/ModifyFunctionPolynomialPieceBegin.cs \
	Cpg.Studio.Undo/ModifyFunctionPolynomialPieceCoefficients.cs \
	Cpg.Studio.Undo/ModifyFunctionPolynomialPieceEnd.cs \
	Cpg.Studio.Undo/ModifyIntegrator.cs \
	Cpg.Studio.Undo/ModifyLinkActionEquation.cs \
	Cpg.Studio.Undo/ModifyLinkActionTarget.cs \
	Cpg.Studio.Undo/ModifyObjectId.cs \
	Cpg.Studio.Undo/ModifyProperty.cs \
	Cpg.Studio.Undo/ModifyProxy.cs \
	Cpg.Studio.Undo/MoveObject.cs \
	Cpg.Studio.Undo/Object.cs \
	Cpg.Studio.Undo/Property.cs \
	Cpg.Studio.Undo/RemoveFunctionPolynomialPiece.cs \
	Cpg.Studio.Undo/RemoveInterfaceProperty.cs \
	Cpg.Studio.Undo/RemoveLinkAction.cs \
	Cpg.Studio.Undo/RemoveObject.cs \
	Cpg.Studio.Undo/RemoveProperty.cs \
	Cpg.Studio.Undo/Template.cs \
	Cpg.Studio.Undo/UnapplyTemplate.cs \
	Cpg.Studio.Undo/Ungroup.cs \
	Cpg.Studio/Utils.cs \
	Cpg.Studio.Widgets/AddRemovePopup.cs \
	Cpg.Studio.Widgets/Annotation.cs \
	Cpg.Studio.Widgets/FunctionNode.cs \
	Cpg.Studio.Widgets/FunctionPolynomialNode.cs \
	Cpg.Studio.Widgets/FunctionPolynomialPieceNode.cs \
	Cpg.Studio.Widgets/FunctionsHelper.cs \
	Cpg.Studio.Widgets/FunctionsView.cs \
	Cpg.Studio.Widgets/GenericFunctionNode.cs \
	Cpg.Studio.Widgets/Graph.cs \
	Cpg.Studio.Widgets/Grid.cs \
	Cpg.Studio.Widgets/MessageArea.cs \
	Cpg.Studio.Widgets/Monitor.cs \
	Cpg.Studio.Widgets/NodeStore.cs \
	Cpg.Studio.Widgets/Notebook.cs \
	Cpg.Studio.Widgets/ObjectView.cs \
	Cpg.Studio.Widgets/Pathbar.cs \
	Cpg.Studio.Widgets/PolynomialsView.cs \
	Cpg.Studio.Widgets/Progress.cs \
	Cpg.Studio.Widgets/PropertyView.cs \
	Cpg.Studio.Widgets/ScrolledWindow.cs \
	Cpg.Studio.Widgets/Table.cs \
	Cpg.Studio.Widgets/TemplatesMenu.cs \
	Cpg.Studio.Widgets/TreeView.cs \
	Cpg.Studio.Widgets/Window.cs \
	Cpg.Studio.Wrappers/Function.cs \
	Cpg.Studio.Wrappers/FunctionPolynomial.cs \
	Cpg.Studio.Wrappers/Graphical.cs \
	Cpg.Studio.Wrappers/Group.cs \
	Cpg.Studio.Wrappers/Link.cs \
	Cpg.Studio.Wrappers/Network.cs \
	Cpg.Studio.Wrappers/Input.cs \
	Cpg.Studio.Wrappers/InputFile.cs \
	Cpg.Studio.Wrappers/Wrapper.cs \
	Cpg.Studio.Wrappers/Object.cs \
	Cpg.Studio.Wrappers/Import.cs

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
	$(CPGNETWORK_SHARP_LIBS) \
	$(BIOROB_MATH_SHARP_LIBS)

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
