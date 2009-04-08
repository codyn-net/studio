using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Components
{
	public class Group : Components.Simulated
	{
		private List<Components.Object> d_children;
		private Components.Simulated d_main;
		private Renderers.Renderer d_renderer;
		private int d_x;
		private int d_y;
		
		public Group(Components.Simulated main) : base()
		{
			d_children = new List<Components.Object>();
			d_main = main;

			d_x = 0;
			d_y = 0;
			
			/* Proxy events */
			if (d_main != null)
			{
				d_main.PropertyAdded += delegate(Object source, string name) {
					DoPropertyAdded(name);
				};
				
				d_main.PropertyChanged += delegate(Object source, string name) {
					DoPropertyChanged(name);
				};
				
				d_main.PropertyRemoved += delegate(Object source, string name) {
					DoPropertyRemoved(name);
				};
				
				d_main.RequestRedraw += delegate(object sender, EventArgs e) {
					DoRequestRedraw();
				};
			}
		}
		
		public Group() : this(null)
		{
		}
		
		public override Cpg.Object Object
		{
			get
			{
				return d_main.Object;
			}
			set
			{
				d_main.Object = value;
			}
		}
		
		public List<Components.Object> Children
		{
			get
			{
				return d_children;
			}
		}
		
		public void Add(Components.Object obj)
		{
			d_children.Add(obj);
		}
		
		public void Remove(Components.Object obj)
		{
			d_children.Remove(obj);
		}
		
		public Renderers.Renderer Renderer
		{
			get
			{
				return d_renderer;	
			}
			set
			{
				d_renderer = value;	
			}
		}
		
		public Components.Simulated Main
		{
			get
			{
				return d_main;
			}
			set
			{
				d_main = value;
			}
		}
		
		public int X
		{
			get
			{
				return d_x;
			}
			set
			{
				d_x = value;
			}
		}
		
		public int Y
		{
			get
			{
				return d_y;
			}
			set
			{
				d_y = value;
			}
		}
		
		/* Proxy to d_main */		
		public override PropertyAttribute FindPropertyAttribute(string name, out PropertyInfo info)
		{
			return d_main.FindPropertyAttribute(name, out info);
		}
		
		public override string ToString()
		{
			return d_main.ToString();
		}
		
		public override bool HasProperty(string name)
		{
			return d_main.HasProperty(name);
		}
		
		public override object GetProperty(string name)
		{
			return d_main.GetProperty(name);
		}
		
		public override void SetProperty(string name, object val)
		{
			d_main.SetProperty(name, val);	
		}
		
		public override void RemoveProperty(string name)
		{
			d_main.RemoveProperty(name);
		}
		
		public override string[] Properties
		{
			get
			{
				return d_main.Properties;
			}
		}
		
		public override List<string> FixedProperties()
		{
			return d_main.FixedProperties();
		}
		
		public override void Draw(System.Drawing.Graphics graphics, System.Drawing.Font font)
		{
			if (d_renderer != null)
				d_renderer.Draw(graphics, font);
			else
				base.Draw(graphics, font);
		}

	}
}
