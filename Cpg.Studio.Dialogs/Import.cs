using System;
using Gtk;

namespace Cpg.Studio.Dialogs
{
	public class Import : FileChooserDialog
	{
		private ComboBox d_comboAction;
		private ComboBox d_comboSelection;

		public Import(Widgets.Window parent) : base("Import CPG Network", parent, FileChooserAction.Open)
		{
			AddButton("Import", ResponseType.Ok);
			AddButton(Gtk.Stock.Cancel, ResponseType.Cancel);
			
			LocalOnly = true;
			SelectMultiple = true;
			
			string path = parent.Network.Path;
			
			if (path != null)
			{
				SetCurrentFolder(System.IO.Path.GetDirectoryName(path));
			}
			
			FileFilter cpg = new FileFilter();
			cpg.AddPattern("*.cpg");
			cpg.Name = "CPG Files (*.cpg)";

			AddFilter(cpg);
			
			FileFilter all = new FileFilter();
			all.AddPattern("*");
			all.Name = "All Files (*)";
			
			AddFilter(all);
			
			Table table = new Table(2, 2, false);
			table.ColumnSpacing = 6;
			table.RowSpacing = 3;

			Label lbl;
			
			lbl = new Label("Action:");
			lbl.SetAlignment(0f, 0.5f);
			table.Attach(lbl, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			
			lbl = new Label("Selection:");
			lbl.SetAlignment(0f, 0.5f);
			table.Attach(lbl, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			
			d_comboAction = new ComboBox(new string[] {"Copy objects from file", "Reference objects in file"});
			table.Attach(d_comboAction, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
			
			d_comboAction.Changed += HandleActionChanged;
			
			d_comboSelection = new ComboBox(new string[] {"All objects", "Only templates"});
			table.Attach(d_comboSelection, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
			
			d_comboSelection.Changed += HandleSelectionChanged;
			
			table.ShowAll();
			
			ExtraWidget = table;
		
			d_comboAction.Active = 0;			
			d_comboSelection.Active = 0;
		}
		
		public bool CopyObjects
		{
			get
			{
				return d_comboAction.Active == 0;
			}
		}
		
		public bool ImportAll
		{
			get
			{
				return d_comboSelection.Active == 0;
			}
		}

		private void HandleSelectionChanged(object sender, EventArgs e)
		{
			if (ImportAll)
			{
				d_comboSelection.TooltipText = "Import all objects, functions and templates from the selected files";
			}
			else
			{
				d_comboSelection.TooltipText = "Import only the templates (and needed functions) from the selected files";
			}
		}

		private void HandleActionChanged(object sender, EventArgs e)
		{
			if (CopyObjects)
			{
				d_comboAction.TooltipText = "Copy (duplicate) objects from the selected files";
			}
			else
			{
				d_comboAction.TooltipText = "Create references to the imported objects. The selected files will be referenced in the network and are direct dependencies";
			}
		}
	}
}