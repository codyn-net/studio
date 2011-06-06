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
			MouseFocus = 1 << 2,
			LinkFocus = 1 << 3,
			Invisible = 1 << 4,
			SelectedAlt = 1 << 5
		}
			
		public event EventHandler RequestRedraw = delegate {};
		public event EventHandler Moved = delegate {};

		private Allocation d_allocation;
		private Renderers.Renderer d_renderer;
		
		private State d_state;
		private Allocation d_lastExtents;
		
		private Dictionary<State, RenderCache> d_cache;
		
		public Graphical()
		{
			Allocation = new Allocation(0f, 0f, 1f, 1f);
			d_state = State.None;
			d_cache = new Dictionary<State, RenderCache>();
		}

		protected virtual bool FromState(State field)
		{
			return (d_state & field) != State.None;
		}
		
		protected virtual bool ToState(State field, bool val)
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
		
		public bool SelectedAlt
		{
			get { return FromState(State.SelectedAlt); }
			set { ToState(State.Selected | State.SelectedAlt, value); }
		}
		
		public bool Selected
		{
			get { return FromState(State.Selected); }
			set { ToState(value ? State.Selected : (State.Selected | State.SelectedAlt), value); }
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
		
		public bool LinkFocus
		{
			get { return FromState(State.LinkFocus); }
			set { ToState(State.LinkFocus, value); }
		}
		
		public bool Invisible
		{
			get { return FromState(State.Invisible); }
			set { ToState(State.Invisible, value); }
		}
		
		public State StateFlags
		{
			set
			{
				if (d_state != value)
				{
					d_state = value;
					DoRequestRedraw();
				}
			}
			get
			{
				return d_state;
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
				if (d_allocation != null)
				{
					d_allocation.Changed -= OnAllocationChanged;
				}

				d_allocation = value;
				
				if (d_allocation != null)
				{
					d_allocation.Changed += OnAllocationChanged;
				}
				
				Moved(this, new EventArgs());
			}
		}
		
		private void OnAllocationChanged(object source, EventArgs args)
		{
			Moved(this, new EventArgs());
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
		
		public virtual void Clicked(Point position)
		{
			// NOOP
		}
		
		protected virtual void DrawSelection(Cairo.Context graphics)
		{
			double alpha = 0.2;
			
			if ((d_state & State.SelectedAlt) == 0)
			{
				graphics.SetSourceRGBA(0, 0, 1, alpha);
			}
			else
			{
				graphics.SetSourceRGBA(1, 0, 0, alpha);
			}

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
			
			double w = Allocation.Width;
			double h = Allocation.Height;
			
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
		
		protected virtual string Label
		{
			get
			{
				return ToString();
			}
		}
		
		protected virtual void DrawLabel(Cairo.Context graphics)
		{
			string s = Label;

			if (String.IsNullOrEmpty(s))
			{
				return;
			}

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
		
		private RenderCache Cache
		{
			get
			{
				RenderCache ret;
				
				if (!d_cache.TryGetValue(d_state, out ret))
				{
					ret = new RenderCache(10);
					d_cache[d_state] = ret;
				}
				
				return ret;
			}
		}

		public virtual void Draw(Cairo.Context context)
		{
			if (Invisible)
			{
				return;
			}

			if (Renderer != null)
			{
				Renderer.Draw(context);
			}
			
			DrawLabel(context);
			
			Cache.Render(context, Allocation.Width, Allocation.Height, delegate (Cairo.Context graphics, double width, double height) {
				if (Selected)
				{
					DrawSelection(graphics);
				}
			
				if (KeyFocus)
				{
					DrawFocus(graphics);
				}
			});
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
		
		public void MoveRel(double x, double y)
		{
			Move(d_allocation.X + x, d_allocation.Y + y);
		}
		
		public void Move(double x, double y)
		{
			Moved(this, new EventArgs());
			d_allocation.Move(x, y);
			
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
		
		public virtual Allocation Extents(double scale, Cairo.Context graphics)
		{
			Allocation alloc = d_allocation.Copy();
			alloc.Scale(scale);
			alloc.GrowBorder(3);
			
			string lbl = Label;

			if (!String.IsNullOrEmpty(lbl))
			{
				int width, height;
				MeasureString(graphics, lbl, out width, out height);
			
				alloc.Height += height + 3;
			
				if (width > alloc.Width)
				{
					int off = (int)(width - alloc.Width / 2);
					alloc.X -= off;
					alloc.Width += off * 2;
				}
			}
			
			d_lastExtents = alloc;
			return d_lastExtents.Copy();
		}
		
		public Allocation LastExtents
		{
			get
			{
				return d_lastExtents;
			}
		}
	}
}
