using System;
using System.Drawing;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Wrappers
{
	public abstract class Graphical
	{
		[Flags]
		public enum State
		{
			None = 0,
			Selected = 1 << 0,
			KeyFocus = 1 << 1,
			MouseFocus = 1 << 2
		}
			
		public event EventHandler RequestRedraw = delegate {};
		public event EventHandler Moved = delegate {};

		private Allocation d_allocation;
		private Renderers.Renderer d_renderer;
		
		private State d_state;
		
		public Graphical()
		{
			d_allocation = new Allocation(0f, 0f, 1f, 1f);
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
			{
				d_state |= field;
			}
			else
			{
				d_state &= ~field;
			}
		
			if (d_state != old)
			{
				DoRequestRedraw();
			}
			
			return d_state != old;
		}
		
		public bool Selected
		{
			get { return FromState(State.Selected); }
			set { ToState(State.Selected, value); }
		}
		
		public bool KeyFocus
		{
			get { return FromState(State.KeyFocus); }
			set { ToState(State.KeyFocus, value); }
		}
		
		public bool MouseFocus
		{
			get { return FromState(State.MouseFocus); }
			set { ToState(State.MouseFocus, value); }
		}
		
		public Allocation Allocation
		{
			get { return d_allocation; }
			set { d_allocation = value; }
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
				DoRequestRedraw();
			}
		}
		
		public virtual Type RendererType
		{
			get
			{
				return Renderer.GetType();
			}
			set
			{
				if (value == null)
				{
					Renderer = null;
				}
				else
				{
					ConstructorInfo info = value.GetConstructor(new Type[] {typeof(Wrappers.Wrapper) });
					
					if (info != null)
					{
						Renderer = info.Invoke(new object[] { this }) as Wrappers.Renderers.Renderer;
					}
					else
					{
						Renderer = null;
					}
				}
			}
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
			if (Renderer != null)
			{
				Renderer.Draw(graphics);
			}

			string s = ToString();

			if (s != String.Empty)
			{
				if (MouseFocus)
				{
					graphics.SetSourceRGB(0.3, 0.6, 0.3);
				}
				else
				{
					graphics.SetSourceRGB(0.3, 0.3, 0.3);
				}
							
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
			{
				DrawSelection(graphics);
			}
		
			if (KeyFocus)
			{
				DrawFocus(graphics);
			}
		}
		
		public virtual void DoRequestRedraw()
		{
			RequestRedraw(this, new EventArgs());
		}
		
		public abstract string Id
		{
			get;
			set;
		}
	
		public override string ToString()
		{
			return Id;
		}
		
		public void MoveRel(float x, float y)
		{
			Move(d_allocation.X + x, d_allocation.Y + y);
		}
		
		public void Move(float x, float y)
		{
			Moved(this, new EventArgs());
			d_allocation.Move(x, y);
			Moved(this, new EventArgs());
			
			DoRequestRedraw();
		}
		
		protected void MeasureString(Cairo.Context graphics, string s, out int width, out int height)
		{
			Pango.FontDescription font = Settings.Font;
			Pango.Layout layout = Pango.CairoHelper.CreateLayout(graphics);
			
			layout.FontDescription = font;
			layout.SetText(s);

			layout.GetSize(out width, out height);
			
			height = (int)(height / Pango.Scale.PangoScale);
			width = (int)(width / Pango.Scale.PangoScale);
		}
		
		public virtual Allocation Extents(float scale, Cairo.Context graphics)
		{
			Allocation alloc = new Allocation(d_allocation);
			alloc.Scale(scale);
			alloc.GrowBorder(3);
		
			int width, height;
			MeasureString(graphics, ToString(), out width, out height);
			
			alloc.Height += height + 3;
			
			if (width > alloc.Width)
			{
				int off = (int)(width - alloc.Width / 2);
				alloc.X -= off;
				alloc.Width += off * 2;
			}

			return alloc;
		}
	}
}
