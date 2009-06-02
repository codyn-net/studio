using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Components
{
	public class Group : Components.Simulated
	{
		private List<Components.Object> d_children;
		private Components.Simulated d_main;
		private int d_x;
		private int d_y;
		
		public Group(Components.Simulated main) : base()
		{
			d_children = new List<Components.Object>();

			d_x = 0;
			d_y = 0;
			
			/* Proxy events */
			SetMain(main);
		}
		
		public void SetMain(Components.Simulated main)
		{
			if (d_main != null)
			{
				foreach (string prop in d_main.Properties)
				{
					if (base.IsPermanent(prop))
						continue;
					
					DoPropertyRemoved(prop);
				}
			}
			
			d_main = main;
			
			if (d_main != null)
			{
				if (String.IsNullOrEmpty(Id))
					Id = d_main.Id;
				
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
				
				foreach (string prop in d_main.Properties)
				{
					if (base.IsPermanent(prop))
						continue;
					
					DoPropertyAdded(prop);
				}
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
				if (d_main != null)
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
			obj.Parent = this;
			d_children.Add(obj);
		}
		
		public void Remove(Components.Object obj)
		{
			obj.Parent = null;
			d_children.Remove(obj);
		}
		
		public Components.Simulated Main
		{
			get
			{
				return d_main;
			}
			set
			{
				SetMain(value);
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
		
		public override Renderers.Renderer Renderer
		{
			get 
			{
				Renderers.Renderer renderer = base.Renderer;
				
				return renderer == null ? new Renderers.Default(this) : renderer;
			}
			set
			{
				base.Renderer = value;
			}
		}
		
		/* Proxy to d_main */		
		public override PropertyAttribute FindPropertyAttribute(string name, out PropertyInfo info)
		{
			return d_main.FindPropertyAttribute(name, out info);
		}
		
		public override bool HasProperty(string name)
		{
			return d_main.HasProperty(name);
		}
		
		public override object GetProperty(string name)
		{
			if (base.IsPermanent(name))
			{
				return base.GetProperty(name);
			}
			else
			{
				return d_main.GetProperty(name);
			}
		}
		
		public override void SetProperty(string name, object val)
		{
			if (base.IsPermanent(name))
			{
				base.SetProperty(name, val);
			}
			else
			{
				d_main.SetProperty(name, val);
			}
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
		
		public override bool GetIntegrated(string name)
		{
			return d_main.GetIntegrated(name);
		}

		public override void SetIntegrated(string name, bool integrated)
		{
			d_main.SetIntegrated(name, integrated);
		}
	}
}
