using System;
using Gtk;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Dialogs
{
	public class Interpolate : Dialog
	{
		private class Node : Widgets.Node
		{
			public double d_x;
			public double d_y;

			public Node(double x, double y)
			{
				d_x = x;
				d_y = y;
			}
			
			[Widgets.SortColumn(0)]
			public int SortNode(Node other)
			{
				return d_x.CompareTo(other.d_x);
			}

			[Widgets.NodeColumn(0)]
			public string XText
			{
				get
				{
					return d_x.ToString();
				}
				set
				{
					X = Double.Parse(value);
				}
			}
			
			[Widgets.NodeColumn(1)]
			public string YText
			{
				get
				{
					return d_y.ToString();
				}
				set
				{
					Y = Double.Parse(value);
				}
			}
			
			public double X
			{
				get
				{
					return d_x;
				}
				set
				{
					d_x = value;
					EmitChanged();
				}
			}
			
			public double Y
			{
				get
				{
					return d_y;
				}
				set
				{
					d_y = value;
					EmitChanged();
				}
			}
		}

		private ComboBox d_comboBox;
		private Dictionary<string, Interpolators.IInterpolator> d_interpolators;
		private TreeModelAdapter d_adapter;
		private Widgets.NodeStore<Node> d_store;
		private TreeView d_treeview;
		private Button d_removeButton;
		private CheckButton d_periodic;
		private Interpolators.Interpolation d_interpolation;
		private DrawingArea d_preview;
		private Entry d_period;

		public Interpolate(Gtk.Window parent, Wrappers.FunctionPolynomial polynomial)
		{
			HasSeparator = false;
			BorderWidth = 0;
			TransientFor = parent;
			DestroyWithParent = true;
			
			Title = "Interpolate";
			SetDefaultSize(500, 300);
			
			AddActionWidget(new Gtk.Button(Gtk.Stock.Cancel), ResponseType.Cancel);
			AddActionWidget(new Gtk.Button(Gtk.Stock.Apply), ResponseType.Apply);
			
			ActionArea.ShowAll();
			
			InitUi(polynomial);
			Fill(polynomial);
			
			d_adapter.RowChanged += delegate(object o, RowChangedArgs args) {
				Update();
			};
			
			d_adapter.RowDeleted += delegate(object o, RowDeletedArgs args) {
				Update();
			};
			
			d_adapter.RowInserted += delegate(object o, RowInsertedArgs args) {
				Update();
			};
			
			Update();			
		}
		
		private int DataPointSort(TreeModel model, TreeIter a1, TreeIter a2)
		{
			double p1 = (double)model.GetValue(a1, 0);
			double p2 = (double)model.GetValue(a2, 0);

			return p1.CompareTo(p2);
		}
		
		private void InitUi(Wrappers.FunctionPolynomial polynomial)
		{
			Alignment align = new Alignment(0.5f, 0.5f, 1, 1);
			align.Show();
			align.SetPadding(6, 6, 6, 6);
			
			VBox.PackStart(align, true, true, 0);

			VBox vbox = new VBox(false, 6);
			vbox.Show();

			align.Add(vbox);
			
			HBox hbox = new HBox(false, 3);
			hbox.PackStart(new Label("Interpolator:"), false, false, 0);
			
			d_comboBox = new ComboBox(new string[] {});
			d_interpolators = new Dictionary<string, Interpolators.IInterpolator>();			
			string name = typeof(Interpolators.IInterpolator).Name;
			
			foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
			{
				if (type.GetInterface(name) != null)
				{
					Interpolators.IInterpolator interpolator = (Interpolators.IInterpolator)type.GetConstructor(new Type[] {}).Invoke(new object[] {});

					d_interpolators[interpolator.Name] = interpolator;
					d_comboBox.AppendText(interpolator.Name);
				}
			}
			
			d_comboBox.Active = 0;
			d_comboBox.Show();
			
			hbox.PackStart(d_comboBox, true, true, 0);
			hbox.ShowAll();
			
			vbox.PackStart(hbox, false, false, 0);
			
			d_store = new Widgets.NodeStore<Node>();
			d_adapter = new TreeModelAdapter(d_store);
			d_treeview = new TreeView(d_adapter);
			
			d_treeview.ShowExpanders = false;
			
			d_treeview.Show();
			
			// x column
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;

			renderer.Edited += delegate(object o, EditedArgs args) {
				Node node = (Node)d_store.FindPath(args.Path);
				node.XText = args.NewText;
			};

			TreeViewColumn column = new TreeViewColumn("X", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 50;
			
			d_treeview.AppendColumn(column);
			
			// y column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += delegate(object o, EditedArgs args) {
				Node node = (Node)d_store.FindPath(args.Path);
				node.YText = args.NewText;
			};

			column = new TreeViewColumn("Y", renderer, "text", 1);
			column.Resizable = true;
			column.MinWidth = 50;
			
			d_treeview.AppendColumn(column);
			
			ScrolledWindow vw = new ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(d_treeview);
			vw.Show();

			HPaned hpaned = new HPaned();
			hpaned.Show();
			
			hpaned.Add1(vw);
			
			d_preview = new DrawingArea();
			d_preview.DoubleBuffered = true;
			d_preview.Show();
			
			d_preview.ExposeEvent += HandleExposeEvent;
			
			hpaned.Add2(d_preview);
			hpaned.Position = 120;

			vbox.PackStart(hpaned, true, true, 0);
			
			hbox = new HBox(false, 3);
			d_removeButton = new Button();
			d_removeButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeButton.Sensitive = false;
			d_removeButton.Clicked += DoRemove;
			d_removeButton.ShowAll();

			hbox.PackStart(d_removeButton, false, false, 0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAdd;
			but.ShowAll();

			hbox.PackStart(but, false, false, 0);
			
			d_period = new Entry();
			d_period.WidthChars = 3;
			d_period.Show();
			d_period.Text = polynomial.Period == null ? "0:1" : String.Format("{0}:{1}", polynomial.Period.Begin, polynomial.Period.End);
			d_period.Sensitive = polynomial.Period != null;
			
			hbox.PackEnd(d_period, false, false, 0);
			
			d_periodic = new CheckButton("Periodic:");
			d_periodic.Show();
			d_periodic.Active = polynomial.Period != null;

			hbox.PackEnd(d_periodic, false, false, 0);
			
			hbox.Show();
			
			vbox.PackStart(hbox, false, false, 0);
			
			d_treeview.KeyPressEvent += DoTreeViewKeyPressEvent;
			d_treeview.Selection.Changed += DoSelectionChanged;
			
			d_periodic.Toggled += DoPeriodicToggled;
			
			d_period.FocusOutEvent += delegate {
				Update();
			};
			
			d_period.Activated += delegate {
				Update();
				d_treeview.GrabFocus();
			};
		}

		private void DoPeriodicToggled(object sender, EventArgs e)
		{
			d_period.Sensitive = d_periodic.Active;

			Update();
		}
		
		public Interpolators.IInterpolator Interpolator
		{
			get
			{
				return d_interpolators[d_comboBox.ActiveText];
			}
		}
		
		public Interpolators.Interpolation Interpolation
		{
			get
			{
				if (d_interpolation == null)
				{
					Update();
				}
				
				return d_interpolation;
			}
		}
		
		private double SumCoefficients(double[] coefficients)
		{
			double ret = 0;
			
			foreach (double coef in coefficients)
			{
				ret += coef;
			}
			
			return ret;
		}
		
		private void Fill(Wrappers.FunctionPolynomial polynomial)
		{
			// Generate data points from polynomial pieces
			FunctionPolynomialPiece[] pieces = polynomial.Pieces;
			Wrappers.FunctionPolynomial.PeriodType period = polynomial.Period;

			for (int i = 0; i < pieces.Length; ++i)
			{
				FunctionPolynomialPiece piece = pieces[i];
				
				if (period == null || (piece.Begin >= period.Begin && piece.End <= period.End))
				{
					AddDataPoint(piece.Begin, piece.Coefficients[piece.Coefficients.Length - 1]);
				}
			}
			
			if (pieces.Length != 0)
			{
				FunctionPolynomialPiece last = pieces[pieces.Length - 1];
				
				if (period == null || (last.Begin >= period.Begin && last.End <= period.End))
				{
					AddDataPoint(last.End, SumCoefficients(last.Coefficients));
				}
			}
		}

		private void DoTreeViewKeyPressEvent(object o, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				DoRemove(this, new EventArgs());
			}
		}

		private void DoSelectionChanged(object sender, EventArgs e)
		{
			int num = d_treeview.Selection.CountSelectedRows();

			d_removeButton.Sensitive = num != 0;
		}
		
		private void DoRemove(object sender, EventArgs args)
		{
			TreePath[] paths = d_treeview.Selection.GetSelectedRows();
			Node[] nodes = new Node[paths.Length];
			
			for (int i = 0; i < paths.Length; ++i)
			{
				nodes[i] = (Node)d_store.FindPath(paths[i]);
			}
			
			foreach (Node node in nodes)
			{
				d_store.Remove(node);
			}
		}
		
		private void AddDataPoint(double x, double y)
		{
			TreeIter piter;
			
			if (d_store.Add(new Node(x, y), out piter))
			{
				d_treeview.Selection.SelectIter(piter);
			}
		}
		
		private void DoAdd(object sender, EventArgs args)
		{
			// Add new data point
			if (d_store.Empty)
			{
				AddDataPoint(0, 0);
			}
			else
			{
				Node node = (Node)d_store[d_store.Count - 1];
				
				if (d_store.Count == 1)
				{
					AddDataPoint(node.X + 1, node.Y);
				}
				else
				{
					Node prev = (Node)d_store[d_store.Count - 2];
					AddDataPoint(node.X + (node.X - prev.X), node.Y + (node.Y - prev.Y));
				}
			}
		}
		
		public double[] Period
		{
			get
			{
				if (!d_period.Sensitive)
				{
					return null;
				}

				try
				{
					string[] parts = d_period.Text.Split(new char[] {':'});

					if (parts.Length == 2)
					{
						return new double[] {double.Parse(parts[0]), double.Parse(parts[1])};
					}
					else
					{
						return new double[] {0, double.Parse(parts[0])};
					}
				}
				catch
				{
					return new double[] {0, 1};
				}
			}
		}
		
		private void Update()
		{
			if (d_store.Empty)
			{
				d_interpolation = null;
				Redraw();
				return;
			}

			List<double> x = new List<double>();
			List<double> y = new List<double>();
					
			foreach (Node node in d_store)
			{
				x.Add(node.X);
				y.Add(node.Y);
			}
			
			if (d_periodic.Active)
			{
				double[] period = Period;
				double range = period[1] - period[0];

				// Add virtual points to make it periodic
				double[] ptx = new double[] {x[0], x[1], x[x.Count - 1], x[x.Count - 2]};
				double[] pty = new double[] {y[0], y[1], y[y.Count - 1], y[y.Count - 2]};

				// Add points before
				x.Insert(0, ptx[2] - range);
				x.Insert(0, ptx[3] - range);
				
				y.Insert(0, pty[2]);
				y.Insert(0, pty[3]);
				
				// Add points after
				x.Add(ptx[0] + range);
				x.Add(ptx[1] + range);
				
				y.Add(pty[0]);
				y.Add(pty[1]);
			}
			
			// Do The interpolation
			d_interpolation = Interpolator.Interpolate(x.ToArray(), y.ToArray());
			Redraw();
		}
		
		private void Redraw()
		{
			d_preview.QueueDraw();
		}
		
		private void DrawPiecePoint(Cairo.Context ctx, double x, double y)
		{
			ctx.Arc(x, y, 3, 0, System.Math.PI * 2);
					
			ctx.SetSourceRGB(1, 1, 1);
			ctx.FillPreserve();

			ctx.SetSourceRGB(0, 0, 0);
			ctx.Stroke();
		}
		
		private void DrawPolynomial(Cairo.Context ctx)
		{
			Interpolators.Interpolation interp = d_interpolation;
			
			if (interp == null)
			{
				return;
			}
			
			// Render the different pieces between 0 and 1
			double[] xx = new double[d_preview.Allocation.Width - 4];
			double[] pts = new double[d_preview.Allocation.Width - 4];
			
			double minpt = 0;
			double maxpt = 0;
			
			double[] period = Period;
			
			if (period == null)
			{
				period = new double[] {d_interpolation.Pieces[0].Begin, d_interpolation.Pieces[d_interpolation.Pieces.Count - 1].End};
			}
			
			for (int i = 2; i < d_preview.Allocation.Width - 2; ++i)
			{
				int idx = i - 2;
				double factor = idx / (double)xx.Length;

				xx[idx] = i;
				pts[idx] = interp.Evaluate(period[0] + (period[1] - period[0]) * factor);
				
				if (idx == 0 || pts[idx] < minpt)
				{
					minpt = pts[idx];
				}

				if (idx == 0 || pts[idx] > maxpt)
				{
					maxpt = pts[idx];
				}
			}
			
			// Check for straight line and scale up a bit to prevent artifacts
			if (minpt == maxpt)
			{
				minpt -= 0.1;
				maxpt += 0.1;
			}
			
			double[,] colors = new double[,] {
				{0, 0, 0.5},
				{0, 0.5, 0},
				{0.5, 0, 0},
				{0, 0.5, 0.5},
				{0.5, 0.5, 0},
				{0.5, 0, 0.5}
			};
			
			double scale = -(d_preview.Allocation.Height - 10) / (maxpt - minpt);
			ctx.Translate(0.5, System.Math.Ceiling(maxpt * -scale) + 4.5);

			ctx.SetSourceRGB(colors[0, 0], colors[0, 1], colors[0, 2]);
			
			int interpIdx = 0;
			int colorIdx = 0;

			while (interpIdx < interp.Pieces.Count && interp.Pieces[interpIdx].End < 0)
			{
				++interpIdx;
			}
			
			double scaleX = xx.Length;
			ctx.LineWidth = 2;
			
			for (int i = 0; i < xx.Length; ++i)
			{
				double factor = xx[i] / scaleX;
				double time = period[0] + (period[1] - period[0]) * factor;

				if (interpIdx < interp.Pieces.Count && time > interp.Pieces[interpIdx].End)
				{
					++colorIdx;
					++interpIdx;
					
					int cidx = colorIdx % colors.Length;
					
					if (i != 0)
					{
						ctx.Stroke();
					}

					ctx.SetSourceRGB(colors[cidx, 0], colors[cidx, 1], colors[cidx, 2]);
				}
				
				if (i == 0)
				{
					ctx.MoveTo(xx[i] + 0.5, pts[i] * scale);
				}
				else
				{
					ctx.LineTo(xx[i] + 0.5, pts[i] * scale);
				}
			}
			
			ctx.Stroke();
			
			ctx.LineWidth = 1;
			
			foreach (Interpolators.Interpolation.Piece piece in interp.Pieces)
			{
				if (piece.Begin >= period[0] && piece.Begin <= period[1])
				{
					double x = (piece.Begin - period[0]) / (period[1] - period[0]) * scaleX + 2;
					double y = piece.Coefficients[piece.Coefficients.Length - 1] * scale;
					
					DrawPiecePoint(ctx, x, y);
				}
				else if (piece.Begin > period[1])
				{
					break;
				}
			}
			
			if (interp.Pieces.Count != 0)
			{
				Interpolators.Interpolation.Piece piece = interp.Pieces[interp.Pieces.Count - 1];

				double x = (piece.End - period[0]) / (period[1] - period[0]) * scaleX + 2;
				double y = SumCoefficients(piece.Coefficients) * scale;
				
				DrawPiecePoint(ctx, x, y);
			}
		}

		private void HandleExposeEvent(object o, ExposeEventArgs args)
		{
			//Interpolators.Interpolation interp = Interpolation;
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(d_preview.GdkWindow))
			{
				ctx.Save();
				
				Gdk.CairoHelper.Rectangle(ctx, args.Event.Area);
				ctx.Clip();
				
				ctx.LineWidth = 1;
				
				ctx.Rectangle(0.5, 0.5, d_preview.Allocation.Width - 1, d_preview.Allocation.Height - 1);
				ctx.SetSourceRGB(1, 1, 1);
				ctx.FillPreserve();
				
				ctx.SetSourceRGB(0, 0, 0);
				ctx.Stroke();
				
				DrawPolynomial(ctx);
				
				ctx.Restore();
			};
		}
	}
}
