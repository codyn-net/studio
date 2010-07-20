using System;
using Gtk;

namespace Cpg.Studio.Dialogs
{
	public class Property : Dialog
	{
		Wrappers.Wrapper d_object;
		PropertyView d_view;
		
		public Property(Window parent, Wrappers.Wrapper obj)
		{
			d_object = obj;
			
			DestroyWithParent = true;
			TransientFor = parent;
			HasSeparator = false;
			
			BorderWidth = 12;
			
			if (obj is Wrappers.Link)
			{
				SetDefaultSize(600, 300);
			}
			else
			{
				SetDefaultSize(400, 300);
			}
			
			VBox.Spacing = 6;
			
			d_view = new PropertyView(parent.Actions, d_object);
			VBox.PackStart(d_view, true, true, 0);
			
			d_object.PropertyChanged += delegate(Wrappers.Wrapper source, Cpg.Property name) {
				UpdateTitle();
			};
			
			VBox.ShowAll();
			
			AddButton(Gtk.Stock.Close, ResponseType.Close);
			UpdateTitle();
		}
		
		public PropertyView View
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
