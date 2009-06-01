using System;
using Gtk;

namespace Cpg.Studio
{
	public class PropertyDialog : Dialog
	{
		Components.Object d_object;
		
		public PropertyDialog(Window parent, Components.Object obj)
		{
			d_object = obj;
			
			DestroyWithParent = true;
			TransientFor = parent;
			HasSeparator = false;
			
			BorderWidth = 12;
			
			if (obj is Components.Link)
			{
				SetDefaultSize(600, 300);
			}
			else
			{
				SetDefaultSize(400, 300);
			}
			
			if (obj is Components.Group)
			{
				Components.Group group = obj as Components.Group;
				GroupProperties props = new GroupProperties(group.Children.ToArray(), group.Main, group.Renderer.GetType()); 
				
				VBox.PackStart(props, false, false, 0);
				VBox.PackStart(new HSeparator(), false, false, 0);
				
				props.ComboMain.Changed  += delegate(object sender, EventArgs e) {
					group.Main = props.Main;
				};
				
				props.ComboKlass.Changed += delegate(object sender, EventArgs e) {
					group.RendererType = props.Klass;
				};
			}
			
			VBox.PackStart(new PropertyView(d_object), true, true, 0);
			
			d_object.PropertyChanged += delegate(Components.Object source, string name) {
				UpdateTitle();
			};
			
			VBox.ShowAll();
			
			AddButton(Gtk.Stock.Close, ResponseType.Close);
			UpdateTitle();
		}
		
		private void UpdateTitle()
		{
			Title = d_object.ToString() + " - Properties";
		}
	}
}
