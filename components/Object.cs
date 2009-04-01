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

		private System.Drawing.Rectangle d_allocation;
		private Dictionary<string, object> d_properties;
		
		private bool d_selected;
		
		public Object()
		{
			d_allocation = new System.Drawing.Rectangle(0, 0, 1, 1);
			d_properties = new Dictionary<string, object>();
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
		
		public System.Drawing.Rectangle Allocation
		{
			get
			{
				return d_allocation;
			}
			set
			{
				d_allocation = value;
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
		
		public virtual void Removed()
		{
		}
		
		public virtual bool HitTest(System.Drawing.Rectangle rect)
		{
			return true;
		}
	}
}
