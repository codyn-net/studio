																																																																																																																																								using System;
using Gtk;
using System.Collections.Generic;

namespace Cdn.Studio.Dialogs
{
	public class Physics : Gtk.Window
	{
		private class LineSpec
		{
			private List<Cdn.Variable> d_x;
			private List<Cdn.Variable> d_y;
			private List<Cdn.Object> d_objs;
			private List<List<Biorob.Math.Point>> d_data;
			private Plot.Renderers.Line d_renderer;

			public LineSpec()
			{
				d_x = new List<Cdn.Variable>();
				d_y = new List<Cdn.Variable>();
				d_data = new List<List<Biorob.Math.Point>>();
				d_objs = new List<Object>();
				d_renderer = new Plot.Renderers.Line {LineStyle = Plot.Renderers.LineStyle.Single,
				MarkerStyle = Plot.Renderers.MarkerStyle.FilledCircle, MarkerSize = 10};
			}

			public void Add(Cdn.Object obj, Cdn.Edge link)
			{
				if (link != null && link.Variable("anchor_x") != null && link.Variable("anchor_y") != null)
				{
					d_x.Add(link.Variable("anchor_x"));
					d_y.Add(link.Variable("anchor_y"));

					d_objs.Add(obj);
				}
				else if (obj.Variable("position_x") != null && obj.Variable("position_y") != null)
				{
					d_x.Add(obj.Variable("position_x"));
					d_y.Add(obj.Variable("position_y"));

					d_objs.Add(obj);
				}
			}

			public void Record()
			{
				List<Biorob.Math.Point> data = new List<Biorob.Math.Point>();

				for (int i = 0; i < d_x.Count; ++i)
				{
					data.Add(new Biorob.Math.Point(d_x[i].Value, d_y[i].Value));
				}

				d_data.Add(data);
			}

			public Plot.Renderers.Line Renderer
			{
				get { return d_renderer; }
			}

			public void Draw(int idx)
			{
				if (idx >= d_data.Count)
				{
					return;
				}

				d_renderer.Data = d_data[idx];
			}
		}

		private Network d_network;
		private Plot.Widget d_canvas;
		private List<LineSpec> d_specs;
		private double d_to;
		private double d_step;
		private uint d_timeoutid;
		private Button d_button;
		private CheckButton d_fast;

		public Physics(Network network, Simulation sim) : base("Physics")
		{
			SetDefaultSize(400, 300);

			network.Compiled += HandleNetworkCompiled;
			sim.OnStepped += HandleSimOnStepped;
			sim.OnBegin += HandleSimOnBegin;
			sim.OnEnd += HandleSimOnEnd;

			d_network = network;

			VBox vbox = new VBox(false, 6);
			vbox.Show();

			d_canvas = new Plot.Widget();
			d_canvas.Show();
			d_canvas.Graph.AutoRecolor = false;
			d_canvas.Graph.ShowRuler = false;
			d_canvas.Graph.RulerTracksData = false;
			d_canvas.Graph.XAxisMode = Plot.AxisMode.Fixed;
			d_canvas.Graph.YAxisMode = Plot.AxisMode.Fixed;
			d_canvas.Graph.KeepAspect = true;

			vbox.PackStart(d_canvas, true, true, 0);
			Add(vbox);

			Button button = new Button("Play");
			button.Show();

			HBox hbox = new HBox(false, 6);
			hbox.Show();

			hbox.PackEnd(button, false, false, 0);
			vbox.PackStart(hbox, false, true, 0);

			button.Clicked += HandleButtonClicked;
			d_button = button;
			d_button.Sensitive = false;

			d_fast = new CheckButton("Fast");
			d_button.Sensitive = false;
			hbox.PackEnd(d_fast, false, false, 0);
			d_fast.Show();

			d_fast.Toggled += HandleFastToggled;

			Scan();
		}

		private void HandleFastToggled(object sender, EventArgs e)
		{
			StartAnimation();
		}

		protected override void OnDestroyed()
		{
			if (d_timeoutid != 0)
			{
				GLib.Source.Remove(d_timeoutid);
				d_timeoutid = 0;
			}
		}

		void HandleSimOnEnd(object sender, EventArgs e)
		{
			d_button.Sensitive = true;
		}

		private void HandleSimOnBegin(object o, Cdn.BegunArgs args)
		{
			// TODO
			//d_to = args.To;
			//d_step = args.Step;

			if (d_timeoutid != 0)
			{
				GLib.Source.Remove(d_timeoutid);
				d_timeoutid = 0;
			}

			foreach (Plot.Renderers.Renderer renderer in d_canvas.Graph.Renderers)
			{
				d_canvas.Graph.Remove(renderer);
			}

			d_button.Sensitive = false;
		}

		private void HandleButtonClicked(object sender, EventArgs e)
		{
			StartAnimation();
		}

		private void StartAnimation()
		{
			if (d_timeoutid != 0)
			{
				GLib.Source.Remove(d_timeoutid);
				d_timeoutid = 0;
			}

			double t = 0;

			foreach (LineSpec spec in d_specs)
			{
				d_canvas.Graph.Add(spec.Renderer);
			}

			DrawFrame(t);

			uint ms = 20;
			double dt = ms / 1000.0;

			if (d_fast.Active)
			{
				dt *= 2;
			}

			d_timeoutid = GLib.Timeout.Add(ms, delegate()
			{
				t += dt;

				if (t > d_to)
				{
					return false;
				}

				DrawFrame(t);
				return true;
			});
		}

		private void DrawFrame(double t)
		{
			int idx = (int)(t / d_step);

			foreach (LineSpec spec in d_specs)
			{
				spec.Draw(idx);
			}

			d_canvas.QueueDraw();
		}

		private void HandleSimOnStepped(object o, SteppedArgs args)
		{
			foreach (LineSpec spec in d_specs)
			{
				spec.Record();
			}
		}

		private void HandleNetworkCompiled(object sender, EventArgs e)
		{
			Scan();
		}

		private void Scan()
		{
			d_specs = new List<LineSpec>();
			List<Cdn.Object> objs = new List<Cdn.Object>(d_network.FindObjects("descendants | has-template(\"physics\" . \"body\")"));

			foreach (Cdn.Object o in objs)
			{
				bool didit = false;
				Cdn.Node n = o as Cdn.Node;

				if (n == null)
				{
					continue;
				}

				foreach (Cdn.Edge l in n.Edges)
				{
					if (objs.Contains(l.Input))
					{
						LineSpec spec = new LineSpec();

						spec.Add(l.Input, l);
						spec.Add(o, null);

						d_specs.Add(spec);
						didit = true;
					}
				}

				if (!didit)
				{
					LineSpec spec = new LineSpec();

					spec.Add(o, null);
					d_specs.Add(spec);
				}
			}
		}
	}
}

