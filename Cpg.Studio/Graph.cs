using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cpg.Studio
{
	public class Graph : Gtk.DrawingArea
	{
		private class Ticks
		{
			public double Start;
			public double Width;
			
			public Ticks(double start, double width)
			{
				Start = start;
				Width = width;
			}
		}
		
		public struct Range
		{
			public double Min;
			public double Max;
			
			public Range(double min, double max)
			{
				Min = min;
				Max = max;
			}
			
			public void Widen(double factor)
			{
				double dist = Math.Abs((Max - Min) * factor);
				
				if (dist < 0.00001)
				{
					dist = factor;
				}
				
				Max += dist;
				Min -= dist;
			}
		}
		
		public class Container
		{
			private List<double> d_data;
			private Cairo.Color d_color;
			private string d_label;
			private int d_unprocessed;
			
			public event EventHandler Changed = delegate {};
			
			public Container(double[] data, Cairo.Color color, string label)
			{
				d_data = new List<double>(data);
				d_color = color;
				d_label = label;
				
				d_unprocessed = 0;
			}
			
			public Container(double[] data, Cairo.Color color) : this(data, color, "")
			{
			}
			
			public List<double> Data
			{
				get
				{
					return d_data;
				}
				set
				{
					d_data = value;
					Changed(this, new EventArgs());
				}
			}
			
			public void SetData(double[] data)
			{
				d_data.Clear();
				d_data.AddRange(data);
				Changed(this, new EventArgs());
			}
			
			public Cairo.Color Color
			{
				get
				{
					return d_color;
				}
				set
				{
					d_color = value;
					Changed(this, new EventArgs());
				}
			}
			
			public string Label
			{
				get
				{
					return d_label;
				}
				set
				{
					d_label = value;
					Changed(this, new EventArgs());
				}
			}
			
			public void Append(double pt)
			{
				d_data.Add(pt);
				++d_unprocessed;
			}
			
			public void Processed()
			{
				d_unprocessed = 0;
			}
			
			public int Unprocessed
			{
				get
				{
					return d_unprocessed;
				}
			}
			
			public bool HasData(int idx)
			{
				if (idx < 0)
					idx = d_data.Count + idx;
				
				return idx < d_data.Count;
			}
			
			public double this[int idx]
			{
				get
				{
					if (idx < 0)
						idx = d_data.Count + idx;
					
					if (idx >= d_data.Count)
						return 0;
					else
						return d_data[idx];
				}
			}
		}
		
		private bool d_showRuler;
		private List<Container> d_data;
		private int d_sampleWidth;
		private Range d_yaxis;
		private Gdk.Pixmap[] d_backbuffer;
		private int d_currentBuffer;
		private Point d_ruler;
		private Ticks d_ticks;
		private bool d_recreate;
		private int d_ruleWhich;
		private bool d_hasRuler;
		
		private static Cairo.Color[] s_colors;
		private static int s_colorIndex;
		static Graph()
		{
			s_colors = new Cairo.Color[] {
				new Cairo.Color(0, 0, 0.6),
				new Cairo.Color(0, 0.6, 0),
				new Cairo.Color(0.6, 0, 0),
				new Cairo.Color(0, 0.6, 0.6),
				new Cairo.Color(0.6, 0.6, 0),
				new Cairo.Color(0.6, 0, 0.6),
				new Cairo.Color(0.6, 0.6, 0.6),
				new Cairo.Color(0, 0, 0)
			};
		}
		
		private static Cairo.Color NextColor()
		{
			Cairo.Color ret = s_colors[s_colorIndex];
			
			if (s_colorIndex + 1 == s_colors.Length)
			{
				s_colorIndex = 0;
			}
			else
			{
				++s_colorIndex;
			}
			
			return ret;
		}
		
		public Graph(int sampleWidth, Range yaxis)
		{
			d_showRuler = true;
			d_data = new List<Container>();
			d_sampleWidth = sampleWidth;
			d_yaxis = yaxis;
			
			d_backbuffer = new Gdk.Pixmap[2] {null, null};

			d_currentBuffer = 0;
			d_ruleWhich = 0;
			
			AddEvents((int)(Gdk.EventMask.Button1MotionMask |
			          Gdk.EventMask.ButtonPressMask |
			          Gdk.EventMask.ButtonReleaseMask |
			          Gdk.EventMask.KeyPressMask |
			          Gdk.EventMask.KeyReleaseMask |
			          Gdk.EventMask.PointerMotionMask |
			          Gdk.EventMask.LeaveNotifyMask | 
			          Gdk.EventMask.EnterNotifyMask));

			DoubleBuffered = true;
		}
		
		public Graph() : this(1, new Range(1, -1))
		{
		}
		
		public bool ShowRuler
		{
			get
			{
				return d_showRuler;
			}
			set
			{
				d_showRuler = value;
				QueueDraw();
			}
		}
		
		public Point Ruler
		{
			get
			{
				return d_ruler;
			}
			set
			{
				d_ruler = value;
				d_hasRuler = true;
				QueueDraw();
			}
		}
		
		public bool HasRuler
		{
			get
			{
				return d_hasRuler;
			}
			set
			{
				d_hasRuler = value;
				QueueDraw();
			}
		}
		
		public int SampleWidth
		{
			get
			{
				return d_sampleWidth;
			}
			set
			{
				d_sampleWidth = value;
				Redraw();
			}
		}
		
		public int Count
		{
			get
			{
				return d_data.Count;
			}
		}
		
		public Range YAxis
		{
			get
			{
				return d_yaxis;
			}
			set
			{
				d_yaxis = value;
				
				if (d_yaxis.Min < 0.00001 && d_yaxis.Max < 0.00001)
				{
					d_yaxis.Min = -1;
					d_yaxis.Max = 1;
				}
				else if (Math.Abs(d_yaxis.Min - d_yaxis.Max) < 0.00001)
				{
					d_yaxis.Widen(0.2);
				}
				
				Redraw();
			}
		}
		
		public void AutoAxis()
		{
			Range range = new Range(-3 , 3);
			
			bool isset = false;
			
			foreach (Container cont in d_data)
			{
				double min = Utils.Min(cont.Data);
				double max = Utils.Max(cont.Data);
				
				if (!isset || min < range.Min)
					range.Min = min;
				
				if (!isset || max > range.Max)
					range.Max = max;
				
				isset = true;
			}
			
			double dist = (range.Max - range.Min) / 2;
			Range ax = new Range(range.Min - dist * 0.2, range.Max + dist * 0.2);
			
			YAxis = ax;
			
			Redraw();
		}
		
		public void SetTicks(double width, double start)
		{
			// width is the number of pixels per tick unit
			// start is the tick unit value from the left
			if (width == 0)
				d_ticks = null;
			else
				d_ticks = new Ticks(start, width);
		}
		
		public void SetTicks(int width)
		{
			SetTicks(width, 0);
		}

		public Container Add(double[] data, string label, Cairo.Color color)
		{
			Container ret = new Container(data, color, label);
			d_data.Add(ret);
			
			if (d_ruleWhich < 0)
				d_ruleWhich = 0;
			
			ret.Changed += delegate(object sender, EventArgs e) {
				Redraw();
			};
			
			Redraw();
			return ret;
		}
		
		public Container Add(double[] data, string label)
		{
			return Add(data, label, NextColor());
		}
		
		public Container Add(double[] data)
		{
			return Add(data, "");
		}
		
		public void Remove(Container container)
		{
			d_data.Remove(container);
			
			if (d_ruleWhich >= d_data.Count)
				d_ruleWhich = d_data.Count - 1;

			Redraw();
		}
		
		private PointF Scale
		{
			get
			{
				return new PointF((float)d_sampleWidth,
				                  (float)(-(Allocation.Height - 3) / (d_yaxis.Max - d_yaxis.Min)));
			}
		}
		
		public static Container SelectMaxUnprocessed(IEnumerable<Container> list)
		{
			bool hasitem = false;
			Container best = default(Container);
			
			foreach (Container item in list)
			{
				if (!hasitem)
					best = item;
				else
					best = item.Unprocessed > best.Unprocessed ? item : best;
				
				hasitem = true;
			}
			
			return best;
		}

		public void ProcessAppend()
		{

			Container maxunp = SelectMaxUnprocessed (d_data);
			
			if (maxunp == null || maxunp.Unprocessed == 0)
				return;
			
			int m = maxunp.Unprocessed;
						
			foreach (Container container in d_data)
			{
				double last = container[-1];
				
				for (int i = 0; i < (m - container.Unprocessed); ++i)
				{
					container.Append(last);
				}
			}
			
			RedrawUnprocessed(m);
			
			foreach (Container container in d_data)
			{
				container.Processed();
			}
		}
		
		private float Samples
		{
			get
			{
				return (Allocation.Width - 2) / (float)d_sampleWidth;
			}
		}
		
		private void Prepare(Cairo.Context ctx)
		{
			PointF scale = Scale;
			ctx.Translate(0.5, Math.Ceiling(d_yaxis.Max * -scale.Y) + 1.5);
		}
		
		private void SetGraphLine(Cairo.Context ctx, Container container)
		{
			ctx.SetSourceRGB(container.Color.R, container.Color.G, container.Color.B);
			ctx.LineWidth = 2;
		}
		
		private void DrawTick(Cairo.Context ctx, double wh)
		{
			ctx.MoveTo(wh, 0);
			ctx.LineWidth = 1.5;
			ctx.LineTo(wh, -5.5);
			ctx.Stroke();
		}
		
		private void DrawSmallTick(Cairo.Context ctx, double wh)
		{
			ctx.LineWidth = 1;
			ctx.MoveTo(wh, 0);
			ctx.LineTo(wh, -3);
			ctx.Stroke();
		}
		
		private void DrawXAxis(Cairo.Context ctx, double from)
		{
			int w = Allocation.Width - 2;
			ctx.MoveTo(from, 0);
			
			ctx.SetSourceRGB(0, 0, 0);
			ctx.LineWidth = 1;
			ctx.LineTo(w, 0);
			ctx.Stroke();
			
			// Draw ticks
			if (d_ticks == null)
				return;
			
			double ms = d_ticks.Start + from / d_ticks.Width;
			double smalltick = d_ticks.Width / 10.0;
			
			while (ms < w)
			{
				DrawTick(ctx, ms);
				
				if (smalltick > 4)
				{
					for (int i = 1; i < 10; ++i)
					{
						DrawSmallTick(ctx, ms + i * smalltick);
					}
				}
				
				ms += d_ticks.Width;
			}
		}
		
		private void RedrawUnprocessed(int num)
		{
			if (GdkWindow == null || d_backbuffer[d_currentBuffer] == null)
				return;
			
			PointF scale = Scale;
			Gdk.Pixmap buf = SwapBuffer();
			
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(buf))
			{
				// Draw old backbuffer on it, shifted
				Gdk.CairoHelper.SetSourcePixmap(ctx, buf, -scale.X * num, 0);
				ctx.Paint();
				
				// draw the points we now need to draw, according to new shift
				Prepare(ctx);
				
				int start = ((int)Math.Floor(Samples) - num) - 1;
				DrawXAxis(ctx, scale.X * start);
				
				foreach (Container container in d_data)
				{
					SetGraphLine(ctx, container);
					
					float s = scale.X * start;
					ctx.MoveTo(s, (container[-num - 1]) * scale.Y);
					
					for (int i = num; i >= 1; --i)
					{
						s += scale.X;
						ctx.LineTo(s, container[-i] * scale.Y);
					}
					
					ctx.Stroke();
				}
				
				QueueDraw();
			}
		}
		
		private void ClearBuffer(Gdk.Pixmap buf)
		{
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(buf))
			{
				ctx.SetSourceRGB(1, 1, 1);
				ctx.Paint();
			}
		}
		
		private Gdk.Pixmap SwapBuffer()
		{
			int neg = d_currentBuffer == 0 ? 1 : 0;
			
			if (d_backbuffer[neg] == null)
				d_backbuffer[neg] = CreateBuffer();
			else
				ClearBuffer(d_backbuffer[neg]);
			
			d_currentBuffer = neg;
			return d_backbuffer[neg];
		}
		
		private Gdk.Pixmap CreateBuffer()
		{
			Gdk.Pixmap buf = new Gdk.Pixmap(GdkWindow, Allocation.Width, Allocation.Height);
			
			ClearBuffer(buf);			
			return buf;
		}
		
		private void Redraw()
		{
			if (GdkWindow == null)
				return;

			d_recreate = true;
			QueueDraw();
		}
		
		public int Offset
		{
			get
			{
				return 0;
			}
		}
		
		private void Recreate()
		{
			d_recreate = false;
			d_backbuffer[0] = null;
			d_backbuffer[1] = null;
			
			Gdk.Pixmap buf = SwapBuffer();
			float n = Samples;
			
			// TODO: support adjustments
			int offset = Offset;
			
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(buf))
			{
				Prepare(ctx);
				DrawXAxis(ctx, 0);
				
				PointF scale = Scale;
				
				foreach (Container container in d_data)
				{
					SetGraphLine(ctx, container);
					int start = Math.Max((int)Math.Floor(n) - (container.Data.Count - offset), 0);
					
					if (container.HasData(offset))
					{
						ctx.MoveTo(start * scale.X, container[offset] * scale.Y);
					}
					
					for (int i = offset + 1; i < container.Data.Count; ++i)
					{
						ctx.LineTo((start + i) * scale.X, container.Data[i] * scale.Y);
					}
					
					ctx.Stroke();
				}
			}
		}
		
		private void Clip(Cairo.Context ctx, Gdk.Rectangle area)
		{
			ctx.Rectangle(area.X, area.Y, area.Width, area.Height);
			ctx.Clip();
		}
		
		private void DrawYAxis(Cairo.Context ctx)
		{
			double cx = Allocation.Width - 0.5;
			
			ctx.LineWidth = 1;
			ctx.SetSourceRGB(0, 0, 0);
			//ctx.MoveTo(cx, 0);
			//ctx.LineTo(cx, Allocation.Height);
			//ctx.Stroke();
			
			string ym = (((int)(d_yaxis.Max * 100)) / 100.0).ToString();
			Cairo.TextExtents e = ctx.TextExtents(ym);
			ctx.MoveTo(cx - e.Width - 5, e.Height + 2);
			ctx.ShowText(ym);
			
			ym = (((int)(d_yaxis.Min * 100)) / 100.0).ToString();
			e = ctx.TextExtents(ym);
			ctx.MoveTo(cx - e.Width - 5, Allocation.Height - 2);
			ctx.ShowText(ym);
		}
		
		private void DrawRuler(Cairo.Context ctx)
		{
			ctx.SetSourceRGB(0.5, 0.6, 1);
			ctx.LineWidth = 1;
			ctx.MoveTo(d_ruler.X + 0.5, 0);
			ctx.LineTo(d_ruler.X + 0.5, Allocation.Height);
			ctx.Stroke();
			
			if (d_data.Count == 0)
				return;
			
			Container container = d_data[d_ruleWhich];

			int offset = Offset;
			int start = Math.Max((int)Math.Floor(Samples) - (container.Data.Count - offset), 0);
			
			PointF scale = Scale;
			
			float dp = d_ruler.X / scale.X;
			
			if ((int)Math.Floor(dp) < start)
				return;
			
			int dpb = (int)Math.Floor(dp);
			int dpe = (int)Math.Ceiling(dp);
			float factor = 1;
			
			if (dpb != dpe)
			{
				factor = (dpe - dp) / (float)(dpe - dpb);
			}
			
			int pos1 = dpb + offset - start;
			int pos2 = dpe + offset - start;
			
			double cy = (container[pos1] * factor) + (container[pos2] * (1 - factor));
			
			// First draw label
			string s = cy.ToString("F3");
			Cairo.TextExtents e = ctx.TextExtents(s);
			
			ctx.Rectangle(d_ruler.X + 3, 1, e.Width + 4, e.Height + 4);
			ctx.SetSourceRGBA(1, 1, 1, 0.8);
			ctx.Fill();
			
			ctx.MoveTo(d_ruler.X + 5, 3 + e.Height);
			ctx.SetSourceRGBA(0, 0, 0, 0.8);
			ctx.ShowText(s);
			ctx.Stroke();
			
			Prepare(ctx);
			
			ctx.LineWidth = 1.5;
			ctx.Arc(d_ruler.X, cy * scale.Y, 5, 0, 2 * Math.PI);
			
			ctx.SetSourceRGBA(0.6, 0.6, 1, 0.5);
			ctx.FillPreserve();
			
			ctx.SetSourceRGB(0.5, 0.6, 1);
			ctx.Stroke();
			
		}
		
		private string HexColor(Cairo.Color color)
		{
			return String.Format("#{0:x2}{1:x2}{2:x2}", 
			                     (int)(color.R * 255),
			                     (int)(color.G * 255),
			                     (int)(color.B * 255));
		}
		
		private void DrawLabel(Cairo.Context ctx)
		{
			if (d_data.Count == 0)
				return;
			
			Pango.Layout layout = Pango.CairoHelper.CreateLayout(ctx);
			layout.FontDescription = Cpg.Studio.Settings.Font;
			
			List<string> labels = new List<string>();
			
			foreach (Container container in d_data)
			{
				if (!String.IsNullOrEmpty(container.Label))
				{
					string lbl = System.Security.SecurityElement.Escape(container.Label);
					labels.Add("<span color='" + HexColor(container.Color) + "'>" + lbl + "</span>");
				}
			}
			
			string t = String.Join(", ", labels.ToArray());
			
			if (t == String.Empty)
				return;
			
			layout.SetMarkup(t);
			
			int width, height;
			layout.GetPixelSize(out width, out height);
			
			ctx.Rectangle(1, 1, width + 3, height + 3);
			ctx.SetSourceRGBA(1, 1, 1, 0.7);
			ctx.Fill();
			
			ctx.MoveTo(1, 1);
			ctx.SetSourceRGBA(0, 0, 0, 0.8);
			
			Pango.CairoHelper.ShowLayout(ctx, layout);			
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			if (d_recreate)
				Recreate();
			
			if (d_backbuffer[d_currentBuffer] == null)
				return true;
			
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(GdkWindow))
			{
				ctx.Save();
				
				Clip(ctx, evnt.Area);
				Gdk.CairoHelper.SetSourcePixmap(ctx, d_backbuffer[d_currentBuffer], 0, 0);
				ctx.Paint();
				
				ctx.Restore();
				
				// Paint label
				ctx.Save();
				DrawLabel(ctx);
				ctx.Restore();
				
				// Paint axis
				ctx.Save();
				DrawYAxis(ctx);
				ctx.Restore();
				
				if (d_showRuler && d_hasRuler)
				{
					DrawRuler(ctx);
				}
			}
			
			return true;
		}

		protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
		{
			bool ret = base.OnConfigureEvent(evnt);
			
			d_recreate = true;
			Redraw();
			return ret;
		}
		
		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			bool ret = base.OnScrollEvent(evnt);
			
			int direction;
			
			if (evnt.Direction == Gdk.ScrollDirection.Up)
			{
				direction = -1;
			}
			else if (evnt.Direction == Gdk.ScrollDirection.Down)
			{
				direction = 1;
			}
			else
			{
				return ret;
			}
			
			if ((evnt.State & Gdk.ModifierType.ControlMask) != 0)
			{
				d_ruleWhich += direction;
				
				if (d_ruleWhich < 0)
					d_ruleWhich = d_data.Count - 1;
				else if (d_ruleWhich >= d_data.Count)
					d_ruleWhich = 0;
				
				QueueDraw();
				
				return ret;
			}
			
			double oldsize = d_yaxis.Max - d_yaxis.Min;
			double newsize = oldsize + oldsize * 0.2 * direction;
			
			double factor = newsize / oldsize;
			
			double y = d_yaxis.Max - (evnt.Y / (double)Allocation.Height) * oldsize;
			double cor = y - (y * factor);
			
			Range ax = new Range(0, 0);
			ax.Max = d_yaxis.Max * factor + cor;
			ax.Min = ax.Max - newsize;
			
			YAxis = ax;
			return ret;
		}
		
		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			d_ruler = new Point((int)evnt.X, (int)evnt.Y);
			d_hasRuler = true;
			
			QueueDraw();
			return false;
		}
		
		protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
		{
			d_hasRuler = false;
			
			QueueDraw();
			return base.OnLeaveNotifyEvent(evnt);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			d_hasRuler = true;
			QueueDraw();

			return base.OnEnterNotifyEvent(evnt);
		}

	}
}
