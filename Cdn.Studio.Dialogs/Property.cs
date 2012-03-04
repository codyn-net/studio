using System;
using Gtk;
using Cpg.Studio.Widgets;

namespace Cpg.Studio.Dialogs
{
	public class Property : Dialog
	{
		Wrappers.Wrapper d_object;
		Widgets.Editors.Wrapper d_view;
		
		public Property(Wrappers.Network network, Widgets.Window parent, Wrappers.Wrapper obj)
		{
			d_object = obj;
			
			DestroyWithParent = true;
			TransientFor = parent;
			HasSeparator = false;
			
			if (obj is Wrappers.Link)
			{
				SetDefaultSize(600, 300);
			}
			else
			{
				SetDefaultSize(400, 300);
			}
			
			d_view = new Widgets.Editors.Wrapper(d_object, parent.Actions, network);
			d_view.BorderWidth = 6;
			
			VBox.PackStart(d_view, true, true, 0);
			
			d_object.PropertyChanged += delegate(Wrappers.Wrapper source, Cpg.Property name) {
				UpdateTitle();
			};
			
			VBox.ShowAll();
			
			AddButton(Gtk.Stock.Close, ResponseType.Close);
			UpdateTitle();
		}
		
		public Widgets.Editors.Wrapper View
		{
			get
			{
				return d_view;
			}
		}
		
		private void UpdateTitle()
		{
			Title = d_object.ToString() + " - Properties";
		}
	}
}
