using System;
using System.Drawing;
using System.Collections.Generic;

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

		private Rectangle d_allocation;
		private Dictionary<string, object> d_properties;
		
		private State d_state;
		
		public Object()
		{
			d_allocation = new System.Drawing.Rectangle(0, 0, 1, 1);
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
		
		public Rectangle Allocation
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
		
		public virtual string[] Properties
		{
			get
			{
				string[] ret = new string[d_properties.Count];
				d_properties.Keys.CopyTo(ret, 0);
				
				return ret;
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
		
		public override string ToString()
		{
			return "";
		}
		
		public void RemoveProperty(string name)
		{
			SetProperty(name, null);
		}
		
		public bool IsPermanent(string name)
		{
			// TODO
			return false;
		}
		
		public bool IsReadOnly(string name)
		{
			// TODO
			return false;
		}
		
		public bool IsInvisible(string name)
		{
			// TODO
			return false;
		}
		
		public virtual void Removed()
		{
		}
		
		public virtual bool HitTest(System.Drawing.Rectangle rect)
		{
			return true;
		}
		
		public virtual void Clicked(System.Drawing.Point position)
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
		
		public virtual void Draw(Graphics graphics)
		{
			string s = ToString();

			if (s != String.Empty)
			{
				Font font = SystemFonts.DefaultFont;
				SizeF size = graphics.MeasureString(s, font);

				graphics.DrawString(s, font, SystemBrushes.ControlText, 
				                    (Allocation.Width - size.Width) / 2.0f,
				                    (Allocation.Height - size.Height) / 2.0f);
			}
			
			if (Selected)
				DrawSelection(graphics);
		
			if (KeyFocus)
				DrawFocus(graphics);
		}
	}
}
