using System;
using System.Drawing;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Components
{
	public class Object
	{
		[Flags]
		public enum State
		{
			None = 0,
			Selected = 1 << 0,
			KeyFocus = 1 << 1,
			MouseFocus = 1 << 2
		}
		
		public delegate void PropertyHandler(Components.Object source, string name);
		
		public event EventHandler RequestRedraw = delegate {};
		public event PropertyHandler PropertyAdded = delegate {};
		public event PropertyHandler PropertyRemoved = delegate {};
		public event PropertyHandler PropertyChanged = delegate {};

		private Allocation d_allocation;
		private Dictionary<string, object> d_properties;
		
		private State d_state;
		
		public Object()
		{
			d_allocation = new Allocation(0f, 0f, 1f, 1f);
			d_properties = new Dictionary<string, object>();
			
			d_state = State.None;
		}
		
		public static Components.Object FromCpg(Cpg.Object obj)
		{
			Type type = obj.GetType();
			
			if (type == typeof(Cpg.Relay))
				return new Components.Relay(obj as Cpg.Relay);
			else if (type == typeof(Cpg.State))
				return new Components.State(obj as Cpg.State);
			else if (type == typeof(Cpg.Link))
				return new Components.Link(obj as Cpg.Link);
			else
				return null;
		}
		
		private bool FromState(State field)
		{
			return (d_state & field) != State.None;
		}
		
		private bool ToState(State field, bool val)
		{
			State old = d_state;
			
			if (val)
				d_state |= field;
			else
				d_state &= ~field;
		
			if (d_state != old)
				RequestRedraw(this, new EventArgs());
			
			return d_state != old;
		}
				
		public bool Selected
		{
			get
			{
				return FromState(State.Selected);
			}
			set
			{
				ToState(State.Selected, value);
			}
		}
		
		public bool KeyFocus
		{
			get
			{
				return FromState(State.KeyFocus);
			}
			set
			{
				ToState(State.KeyFocus, value);
			}
		}
		
		public bool MouseFocus
		{
			get
			{
				return FromState(State.MouseFocus);
			}
			set
			{
				ToState(State.MouseFocus, value);
			}
		}
		
		public Allocation Allocation
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
		
		public virtual List<string> FixedProperties()
		{
			List<string> lst = new List<string>();
			
			foreach (PropertyInfo info in GetType().GetProperties())
			{
				foreach (object attr in info.GetCustomAttributes(typeof(PropertyAttribute), true))
					lst.Add((attr as PropertyAttribute).Name);
			}
			
			return lst;
		}
		
		public virtual string[] Properties
		{
			get
			{
				List<string> props = FixedProperties();
				
				string[] ret = new string[d_properties.Count + props.Count];
				props.CopyTo(ret, 0);
				d_properties.Keys.CopyTo(ret, props.Count);
				
				return ret;
			}
		}
		
		public virtual bool HasProperty(string name)
		{
			return d_properties.ContainsKey(name) || FindPropertyAttribute(name) != null;
		}
		
		public virtual object GetProperty(string name)
		{
			PropertyInfo info;
			PropertyAttribute attr = FindPropertyAttribute(name, out info);
			
			if (attr != null)
				return info.GetGetMethod().Invoke(this, new object[] {});
			else if (d_properties.ContainsKey(name))
				return d_properties[name];
			else
				return null;
		}
		
		protected virtual void SetPropertyReal(string name, object val)
		{
			if (val == null)
				d_properties.Remove(name);
			else
				d_properties[name] = val;
		}
		
		public virtual void SetProperty(string name, object val)
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
		
		public override string ToString()
		{
			return "";
		}
		
		public virtual void RemoveProperty(string name)
		{
			SetProperty(name, null);
		}
		
		public virtual PropertyAttribute FindPropertyAttribute(string name, out PropertyInfo info)
		{
			Type type = GetType();
			
			PropertyInfo[] infos = type.GetProperties();
			
			foreach (PropertyInfo inf in infos)
			{
				object[] attributes = inf.GetCustomAttributes(typeof(PropertyAttribute), true);
				
				foreach (object attr in attributes)
				{
					PropertyAttribute prop = attr as PropertyAttribute;
					
					if (prop.Name == name)
					{
						info = inf;
						return prop;
					}
				}
			}
			
			info = null;
			return null;
		}
		
		public PropertyAttribute FindPropertyAttribute(string name)
		{
			PropertyInfo info;
			
			return FindPropertyAttribute(name, out info);
		}
		
		public bool IsPermanent(string name)
		{
			PropertyAttribute prop = FindPropertyAttribute(name);
			return prop != null;
		}
		
		public bool IsReadOnly(string name)
		{
			PropertyAttribute prop = FindPropertyAttribute(name);
			return prop != null && prop.ReadOnly;
		}
		
		public bool IsInvisible(string name)
		{
			PropertyAttribute prop = FindPropertyAttribute(name);
			return prop != null && prop.Invisible;
		}
		
		public virtual void Removed()
		{
		}
		
		public virtual bool HitTest(Allocation rect)
		{
			return true;
		}
		
		public virtual void Clicked(PointF position)
		{
			// NOOP
		}
		
		protected virtual void DrawSelection(Graphics graphics)
		{
			SolidBrush br = new SolidBrush(Color.FromArgb(50, 0, 0, 255));
			graphics.FillRectangle(br, 0, 0, Allocation.Width, Allocation.Height);
			
			float scale = Utils.TransformScale(graphics.Transform);
			
			Pen pen = new Pen(Color.FromArgb(50, 0, 0, 0), 1 / scale);
			graphics.DrawRectangle(pen, 1, 1, Allocation.Width, Allocation.Height);
		}
		
		protected virtual void DrawFocus(Graphics graphics)
		{
			// TODO
		}
		
		public virtual void Draw(Graphics graphics, Font font)
		{
			string s = ToString();

			if (s != String.Empty)
			{
				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				
				graphics.DrawString(s, font, SystemBrushes.ControlText, Allocation.Width / 2, font.GetHeight(), format); 
			}
			
			if (Selected)
				DrawSelection(graphics);
		
			if (KeyFocus)
				DrawFocus(graphics);
		}
		
		protected void DoRequestRedraw()
		{
			RequestRedraw(this, new EventArgs());
		}
		
		protected void DoPropertyAdded(string name)
		{
			PropertyAdded(this, name);
		}
		
		protected void DoPropertyChanged(string name)
		{
			PropertyChanged(this, name);
		}
		
		protected void DoPropertyRemoved(string name)
		{
			PropertyRemoved(this, name);
		}
	}
}
