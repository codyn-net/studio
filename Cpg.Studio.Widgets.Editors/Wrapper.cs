using System;

namespace Cpg.Studio.Widgets.Editors
{
	public class Wrapper : Gtk.VBox
	{
		public delegate void TemplateHandler(object source, Wrappers.Wrapper template);
		public event TemplateHandler TemplateActivated = delegate {};

		public delegate void ErrorHandler(object source, Exception exception);
		public event ErrorHandler Error = delegate {};

		private Wrappers.Wrapper d_wrapper;
		private Actions d_actions;
		private Properties d_properties;
		private Wrappers.Network d_network;

		public Wrapper(Wrappers.Wrapper wrapper, Actions actions, Wrappers.Network network) : base(false, 6)
		{
			d_wrapper = wrapper;
			d_actions = actions;
			d_network = network;
			
			Build();
		}
		
		private void Build()
		{
			foreach (Gtk.Widget w in Children)
			{
				w.Destroy();
			}

			Object obj = new Object(d_wrapper, d_actions, d_network);
			
			obj.Error += delegate(object source, Exception exception) {
				Error(source, exception);
			};
			
			obj.TemplateActivated += delegate(object source, Wrappers.Wrapper template) {
				TemplateActivated(source, template);
			};
			
			d_properties = new Properties(d_wrapper, d_actions);
			
			d_properties.Error += delegate(object source, Exception exception) {
				Error(source, exception);
			};
			
			obj.Show();
			d_properties.Show();
			
			Gtk.HBox top = new Gtk.HBox(false, 6);
			top.Show();
			
			top.PackStart(obj, true, true, 0);
			
			Wrappers.Group grp = d_wrapper as Wrappers.Group;
			
			if (grp != null)
			{
				Group gp = new Group(grp, d_actions);
				gp.Show();
				
				top.PackStart(gp, false, false, 0);
			}

			PackStart(top, false, false, 0);
			
			Wrappers.Link link = d_wrapper as Wrappers.Link;
			
			if (link != null)
			{
				Gtk.HPaned paned = new Gtk.HPaned();
				paned.Show();

				paned.Pack1(d_properties, true, true);
				
				Link actions = new Link(link, d_actions);
				actions.Show();
				
				paned.Pack2(actions, true, true);
				
				PackStart(paned, true, true, 0);
			}
			else
			{
				PackStart(d_properties, true, true, 0);
			}
		}
		
		public Wrappers.Wrapper Object
		{
			get
			{
				return d_wrapper;
			}
			set
			{
				d_wrapper = value;
				
				Build();
				
				Sensitive = (d_wrapper != null);
			}
		}
		
		public void Select(Cpg.Property property)
		{
			/* TODO */
		}
		
		public void Select(Cpg.LinkAction action)
		{
			/* TODO */
		}
	}
}

