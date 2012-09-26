

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = $(CMCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:3 -optimize+ -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/cdnstudio.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =
BUILD_DIR = bin/Debug

CDNSTUDIO_EXE_MDB_SOURCE=bin/Debug/cdnstudio.exe.mdb
CDNSTUDIO_EXE_MDB=$(BUILD_DIR)/cdnstudio.exe.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = $(CMCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = bin/Release/cdnstudio.exe
ASSEMBLY_MDB =
COMPILE_TARGET = exe
PROJECT_REFERENCES =
BUILD_DIR = bin/Release

CDNSTUDIO_EXE_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(CDNSTUDIO_EXE_MDB)

BINARIES = \
	$(CDNSTUDIO)


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES)

FILES = \
	Cdn.Studio/Actions.cs \
	Cdn.Studio/Allocation.cs \
	Cdn.Studio/Anchor.cs \
	Cdn.Studio/Application.cs \
	Cdn.Studio/AssemblyInfo.cs \
	Cdn.Studio.Clipboard/Internal.cs \
	Cdn.Studio/Config.cs \
	Cdn.Studio.Dialogs/Editor.cs \
	Cdn.Studio.Dialogs/FindTemplate.cs \
	Cdn.Studio.Dialogs/Import.cs \
	Cdn.Studio.Dialogs/Physics.cs \
	Cdn.Studio.Dialogs/PlotSettings.cs \
	Cdn.Studio.Dialogs/Plotting.cs \
	Cdn.Studio.Dialogs/Variable.cs \
	Cdn.Studio/DynamicIntegrator.cs \
	Cdn.Studio/RenderCache.cs \
	Cdn.Studio.Renderers/Box.cs \
	Cdn.Studio.Renderers/Edge.cs \
	Cdn.Studio.Renderers/Function.cs \
	Cdn.Studio.Renderers/Node.cs \
	Cdn.Studio.Renderers/PiecewisePolynomial.cs \
	Cdn.Studio.Renderers/Renderer.cs \
	Cdn.Studio.Serialization/Project.cs \
	Cdn.Studio/Settings.cs \
	Cdn.Studio/Simulation.cs \
	Cdn.Studio/SimulationRange.cs \
	Cdn.Studio/Stock.cs \
	Cdn.Studio.Undo/AddEdgeAction.cs \
	Cdn.Studio.Undo/AddFunctionArgument.cs \
	Cdn.Studio.Undo/AddFunctionPolynomialPiece.cs \
	Cdn.Studio.Undo/AddInterfaceProperty.cs \
	Cdn.Studio.Undo/AddNode.cs \
	Cdn.Studio.Undo/AddObject.cs \
	Cdn.Studio.Undo/AddVariable.cs \
	Cdn.Studio.Undo/ApplyTemplate.cs \
	Cdn.Studio.Undo/AttachEdge.cs \
	Cdn.Studio.Undo/EdgeAction.cs \
	Cdn.Studio.Undo/FunctionArgument.cs \
	Cdn.Studio.Undo/Function.cs \
	Cdn.Studio.Undo/FunctionPolynomialPiece.cs \
	Cdn.Studio.Undo/IAction.cs \
	Cdn.Studio.Undo/Import.cs \
	Cdn.Studio.Undo/InterfaceVariable.cs \
	Cdn.Studio.Undo/Manager.cs \
	Cdn.Studio.Undo/ModifyEdgeActionEquation.cs \
	Cdn.Studio.Undo/ModifyEdgeActionTarget.cs \
	Cdn.Studio.Undo/ModifyExpression.cs \
	Cdn.Studio.Undo/ModifyFunctionArgumentDefaultValue.cs \
	Cdn.Studio.Undo/ModifyFunctionArgumentExplicit.cs \
	Cdn.Studio.Undo/ModifyFunctionArgumentName.cs \
	Cdn.Studio.Undo/ModifyFunctionArguments.cs \
	Cdn.Studio.Undo/ModifyFunctionPolynomialPieceBegin.cs \
	Cdn.Studio.Undo/ModifyFunctionPolynomialPieceCoefficients.cs \
	Cdn.Studio.Undo/ModifyFunctionPolynomialPieceEnd.cs \
	Cdn.Studio.Undo/ModifyIntegrator.cs \
	Cdn.Studio.Undo/ModifyObjectId.cs \
	Cdn.Studio.Undo/ModifyVariable.cs \
	Cdn.Studio.Undo/MoveObject.cs \
	Cdn.Studio.Undo/Group.cs \
	Cdn.Studio.Undo/Object.cs \
	Cdn.Studio.Undo/RemoveEdgeAction.cs \
	Cdn.Studio.Undo/RemoveFunctionArgument.cs \
	Cdn.Studio.Undo/RemoveFunctionPolynomialPiece.cs \
	Cdn.Studio.Undo/RemoveInterfaceVariable.cs \
	Cdn.Studio.Undo/RemoveObject.cs \
	Cdn.Studio.Undo/RemoveVariable.cs \
	Cdn.Studio.Undo/Template.cs \
	Cdn.Studio.Undo/UnapplyTemplate.cs \
	Cdn.Studio.Undo/Ungroup.cs \
	Cdn.Studio.Undo/Variable.cs \
	Cdn.Studio/Utils.cs \
	Cdn.Studio.Widgets/AboutDialog.cs \
	Cdn.Studio.Widgets/AddRemovePopup.cs \
	Cdn.Studio.Widgets/Annotation.cs \
	Cdn.Studio.Widgets.Editors/Edge.cs \
	Cdn.Studio.Widgets.Editors/Function.cs \
	Cdn.Studio.Widgets.Editors/Object.cs \
	Cdn.Studio.Widgets.Editors/PiecewisePolynomial.cs \
	Cdn.Studio.Widgets.Editors/Variables.cs \
	Cdn.Studio.Widgets.Editors/Wrapper.cs \
	Cdn.Studio.Widgets/GenericFunctionNode.cs \
	Cdn.Studio.Widgets/Grid.cs \
	Cdn.Studio.Widgets/IDragIcon.cs \
	Cdn.Studio.Widgets/MessageArea.cs \
	Cdn.Studio.Widgets/NodeStore.cs \
	Cdn.Studio.Widgets/Notebook.cs \
	Cdn.Studio.Widgets/Pathbar.cs \
	Cdn.Studio.Widgets/Progress.cs \
	Cdn.Studio.Widgets/ScrolledWindow.cs \
	Cdn.Studio.Widgets/Table.cs \
	Cdn.Studio.Widgets/TemplatesMenu.cs \
	Cdn.Studio.Widgets/TreeView.cs \
	Cdn.Studio.Widgets/Window.cs \
	Cdn.Studio.Widgets/WrappersTree.cs \
	Cdn.Studio.Wrappers/Edge.cs \
	Cdn.Studio.Wrappers/Function.cs \
	Cdn.Studio.Wrappers/FunctionPolynomial.cs \
	Cdn.Studio.Wrappers/Graphical.cs \
	Cdn.Studio.Wrappers/ImportAlias.cs \
	Cdn.Studio.Wrappers/Import.cs \
	Cdn.Studio.Wrappers/Network.cs \
	Cdn.Studio.Wrappers/Node.cs \
	Cdn.Studio.Wrappers/Object.cs \
	Cdn.Studio.Wrappers/Wrapper.cs

DATA_FILES =

RESOURCES = \
	Cdn.Studio.Resources/ui.xml \
	Cdn.Studio.Resources/chain.png \
	Cdn.Studio.Resources/chain-broken.png \
	Cdn.Studio.Resources/plotting-ui.xml

EXTRAS = \
	cdnstudio.in

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GTKSOURCEVIEW_SHARP_20_LIBS) \
	System \
	Mono.Posix \
	System.Drawing \
	Mono.Cairo \
	System.Xml \
	System.Core \
	$(CDNNETWORK_SHARP_LIBS) \
	$(BIOROB_MATH_SHARP_LIBS) \
	$(PLOT_SHARP_LIBS)

DLL_REFERENCES =

CLEANFILES = $(PROGRAMFILES) $(BINARIES)

include $(top_srcdir)/Makefile.include

CDNSTUDIO = $(BUILD_DIR)/cdn-studio

$(eval $(call emit-deploy-wrapper,CDNSTUDIO,cdn-studio,x))


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
