using System;
using System.Collections.Generic;

namespace Cpg.Studio.Components
{
	public class Object
	{
		public delegate void PropertyHandler(Components.Object source, string name);
		
		public event EventHandler RequestRedraw;
		public event PropertyHandler PropertyAdded;
		public event PropertyHandler PropertyRemoved;
		public event PropertyHandler PropertyChanged;

		private Grid d_grid;
		private Allocation d_allocation;
		private Dictionary<string, object> d_properties;
		
		private bool d_selected;
		
		public Object(Grid grid)
		{
			d_grid = grid;
			d_allocation = new Cpg.Studio.Allocation();
			d_properties = new Dictionary<string, object>();
		}
		
		public Object() : this(null)
		{
		}
		
		public bool Selected
		{
			get
			{
				return d_selected;
			}
			set
			{
				d_selected = value;
			}
		}
		
		public Grid Grid
		{
			get
			{
				return d_grid;
			}
			set
			{
				if (d_grid == value)
					return;

				Removed();
				d_grid = value;
			}
		}
		
		public Allocation Allocation
		{
			get
			{
				return d_allocation;
			}
		}
		
		public string this[string name]
		{
			get
			{
				return GetProperty(name) as string;
			}
			set
			{
				SetProperty(name, value);
			}
		}
		
		public virtual bool HasProperty(string name)
		{
			return d_properties.ContainsKey(name);
		}
		
		public virtual object GetProperty(string name)
		{
			return d_properties[name];
		}
		
		protected virtual void SetPropertyReal(string name, object val)
		{
			if (val == null)
				d_properties.Remove(name);
			else
				d_properties[name] = val;
		}
		
		public void SetProperty(string name, object val)
		{
			bool newprop = !HasProperty(name);
			
			SetPropertyReal(name, val);
			
			if (newprop && val != null)
				PropertyAdded(this, name);
			else if (!newprop && val != null)
				PropertyChanged(this, name);
			else if (!newprop && val == null)
				PropertyRemoved(this, name);
		}
		
		public void RemoveProperty(string name)
		{
			SetProperty(name, null);
		}
		
		protected virtual void Removed()
		{
		}
	}
}
