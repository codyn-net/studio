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
		private Renderers.Renderer d_renderer;
		private Components.Group d_parent;
		private string d_id;
		
		private State d_state;
		
		public Object()
		{
			d_allocation = new Allocation(0f, 0f, 1f, 1f);
			d_properties = new Dictionary<string, object>();
			
			d_state = State.None;
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
		
		public virtual Renderers.Renderer Renderer
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
		
		public Type RendererType
		{
			get
			{
				return d_renderer.GetType();
			}
			set
			{
				if (value == null)
				{
					d_renderer = null;
				}
				else
				{
					ConstructorInfo info = value.GetConstructor(new Type[] {typeof(Components.Object) });
					
					if (info != null)
					{
						d_renderer = info.Invoke(new object[] { this }) as Components.Renderers.Renderer;
					}
					else
					{
						Console.WriteLine(value);
						d_renderer = null;
					}
				}
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
		
		public virtual Components.Group Parent
		{
			get
			{
				return d_parent;
			}
			set
			{
				d_parent = value;
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
			
			if (!newprop && val != null && GetProperty(name).Equals(val))
				return;

			SetPropertyReal(name, val);
			
			if (newprop && val != null)
				PropertyAdded(this, name);
			else if (!newprop && val != null)
				PropertyChanged(this, name);
			else if (!newprop && val == null)
				PropertyRemoved(this, name);
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
		
		protected virtual void DrawSelection(Cairo.Context graphics)
		{
			double alpha = 0.2;
			
			graphics.SetSourceRGBA(0, 0, 1, alpha);
			graphics.Rectangle(0, 0, Allocation.Width, Allocation.Height);
			graphics.FillPreserve();
			
			graphics.SetSourceRGBA(0, 0, 0, alpha);
			graphics.Stroke();
		}
		
		protected virtual void DrawFocus(Cairo.Context graphics)
		{
			double uw = graphics.LineWidth;
			graphics.LineWidth *= 6;
			
			int fw = 12;
			int dw = 0;
			
			float w = Allocation.Width;
			float h = Allocation.Height;
			
			graphics.SetSourceRGBA(0.6, 0.6, 0.6, 0.5);
			
			graphics.MoveTo(-uw * dw, uw * fw);
			graphics.LineTo(-uw *dw, -uw * dw);
			graphics.LineTo(uw * fw, -uw * dw);
			graphics.Stroke();
			
			graphics.MoveTo(w - uw * fw, -uw * dw);
			graphics.LineTo(w + uw * dw, -uw * dw);
			graphics.LineTo(w + uw * dw, uw * fw);
			graphics.Stroke();
			
			graphics.MoveTo(w + uw * dw, h - uw * fw);
			graphics.LineTo(w + uw * dw, h + uw * dw);
			graphics.LineTo(w - uw * fw, h + uw * dw);
			graphics.Stroke();
			
			graphics.MoveTo(uw * fw, h + uw * dw);
			graphics.LineTo(-uw * dw, h + uw * dw);
			graphics.LineTo(-uw * dw, h - uw * fw);
			graphics.Stroke();
		}
		
		public virtual void Draw(Cairo.Context graphics)
		{
			if (d_renderer != null)
				d_renderer.Draw(graphics);

			string s = ToString();

			if (s != String.Empty)
			{
				if (MouseFocus)
					graphics.SetSourceRGB(0.3, 0.6, 0.3);
				else
					graphics.SetSourceRGB(0.3, 0.3, 0.3);
							
				double uw = graphics.LineWidth;
				
				graphics.Save();
				graphics.Scale(uw, uw);
				
				Pango.Layout layout = Pango.CairoHelper.CreateLayout(graphics);
				Pango.CairoHelper.UpdateLayout(graphics, layout);
				layout.FontDescription = Settings.Font;

				layout.SetText(s);
				
				int width, height;
				layout.GetSize(out width, out height);
				
				width = (int)(width / Pango.Scale.PangoScale);
				
				graphics.MoveTo((Allocation.Width / uw - width) / 2.0, Allocation.Height / uw + 2);
				Pango.CairoHelper.ShowLayout(graphics, layout);
				graphics.Restore();
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
		
		[Property("id")]
		public virtual string Id
		{
			get
			{
				return d_id;
			}
			
			set
			{
				d_id = value;
				DoRequestRedraw();
			}
		}
		
		public override string ToString()
		{
			return Id;
		}
	}
}
