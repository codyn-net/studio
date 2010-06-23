using System;
using Gtk;

namespace Cpg.Studio
{
	public class PropertyDialog : Dialog
	{
		Wrappers.Wrapper d_object;
		PropertyView d_view;
		
		public PropertyDialog(Window parent, Wrappers.Wrapper obj)
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
			
			if (obj is Wrappers.Group)
			{
				Wrappers.Group group = obj as Wrappers.Group;
				GroupProperties props = new GroupProperties(group.Children, group.Proxy, group.Renderer.GetType()); 
				
				VBox.PackStart(props, false, false, 0);
				VBox.PackStart(new HSeparator(), false, false, 0);
				
				props.ComboMain.Changed  += delegate(object sender, EventArgs e) {
					group.SetProxy(props.Main);
				};
				
				props.ComboKlass.Changed += delegate(object sender, EventArgs e) {
					group.RendererType = props.Klass;
				};
			}
			
			d_view = new PropertyView(d_object);
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
