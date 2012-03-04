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
		private Function d_function;
		private PiecewisePolynomial d_piecewise;

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
			
			d_properties = null;
			d_function = null;
			d_piecewise = null;

			Object obj = new Object(d_wrapper, d_actions, d_network);
			obj.Show();
			
			obj.Error += delegate(object source, Exception exception) {
				Error(source, exception);
			};
			
			obj.TemplateActivated += delegate(object source, Wrappers.Wrapper template) {
				TemplateActivated(source, template);
			};

			Gtk.HBox top = new Gtk.HBox(false, 6);
			top.Show();
			
			top.PackStart(obj, true, true, 0);

			if (!(d_wrapper is Wrappers.Function))
			{
				d_properties = new Properties(d_wrapper, d_actions);
				d_properties.Show();
			
				d_properties.Error += delegate(object source, Exception exception) {
					Error(source, exception);
				};
			}
			else if (d_wrapper is Wrappers.FunctionPolynomial)
			{
				d_piecewise = new PiecewisePolynomial(d_wrapper as Wrappers.FunctionPolynomial, d_actions);
				d_piecewise.Show();
				
				top.PackEnd(d_piecewise.PeriodWidget, false, false, 0);
			}
			else
			{
				d_function = new Function(d_wrapper as Wrappers.Function, d_actions);
				d_function.Show();
				
				d_function.Error += delegate(object source, Exception exception) {
					Error(source, exception);
				};
			}
					
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
			else if (d_properties != null)
			{
				PackStart(d_properties, true, true, 0);
			}
			else if (d_function != null)
			{
				PackStart(d_function, true, true, 0);
			}
			else if (d_piecewise != null)
			{
				PackStart(d_piecewise, true, true, 0);
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

