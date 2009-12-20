using System;
using Gtk;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio
{
	public class InterpolateDialog : Dialog
	{
		private ComboBox d_comboBox;
		private Dictionary<string, Interpolators.IInterpolator> d_interpolators;
		private ListStore d_store;
		private TreeView d_treeview;
		private Button d_removeButton;
		private CheckButton d_periodic;
		private Interpolators.Interpolation d_interpolation;
		private DrawingArea d_preview;

		public InterpolateDialog(Gtk.Window parent, Cpg.FunctionPolynomial polynomial)
		{
			HasSeparator = false;
			BorderWidth = 10;
			TransientFor = parent;
			DestroyWithParent = true;
			
			Title = "Interpolate";
			SetDefaultSize(500, 300);
			
			AddActionWidget(new Gtk.Button(Gtk.Stock.Cancel), ResponseType.Cancel);
			AddActionWidget(new Gtk.Button(Gtk.Stock.Apply), ResponseType.Apply);
			
			ActionArea.ShowAll();
			
			InitUi();
			Fill(polynomial);
			
			d_store.RowChanged += delegate(object o, RowChangedArgs args) {
				Update();
			};
			
			d_store.RowDeleted += delegate(object o, RowDeletedArgs args) {
				Update();
			};
			
			d_store.RowInserted += delegate(object o, RowInsertedArgs args) {
				Update();
			};
			
			Update();
			
			d_store.SetSortFunc(0, DataPointSort);
			d_store.SetSortColumnId(0, SortType.Ascending);
		}
		
		private int DataPointSort(TreeModel model, TreeIter a1, TreeIter a2)
		{
			double p1 = (double)model.GetValue(a1, 0);
			double p2 = (double)model.GetValue(a2, 0);

			return p1.CompareTo(p2);
		}
		
		private void InitUi()
		{
			VBox.Spacing = 6;

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
			
			VBox.PackStart(hbox, false, false, 0);
			
			d_store = new ListStore(typeof(double), typeof(double));
			d_treeview = new TreeView(d_store);
			
			d_treeview.Show();
			
			// x column
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += delegate(object o, EditedArgs args) {
				DoEdited(0, args.NewText);
			};

			TreeViewColumn column = new TreeViewColumn("X", renderer);
			column.Resizable = true;
			column.MinWidth = 75;
			
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
				double val = (double)d_store.GetValue(piter, 0);
				(cell as CellRendererText).Text = String.Format("{0}", val);
			});

			d_treeview.AppendColumn(column);
			
			// y column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += delegate(object o, EditedArgs args) {
				DoEdited(1, args.NewText);
			};

			column = new TreeViewColumn("Y", renderer);
			column.Resizable = true;
			column.MinWidth = 75;
			
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter piter) {
				double val = (double)d_store.GetValue(piter, 1);
				(cell as CellRendererText).Text = String.Format("{0}", val);
			});

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
			hpaned.Position = 150;

			VBox.PackStart(hpaned, true, true, 0);
			
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
			
			d_periodic = new CheckButton("Periodic");
			d_periodic.Show();

			hbox.PackEnd(d_periodic, false, false, 0);
			
			hbox.Show();
			
			VBox.PackStart(hbox, false, false, 0);
			
			d_treeview.KeyPressEvent += DoTreeViewKeyPressEvent;
			d_treeview.Selection.Changed += DoSelectionChanged;
			
			d_periodic.Toggled += DoPeriodicToggled;
		}

		private void DoPeriodicToggled(object sender, EventArgs e)
		{
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
		
		private void Fill(Cpg.FunctionPolynomial polynomial)
		{
			// Generate data points from polynomial pieces
			FunctionPolynomialPiece[] pieces = polynomial.Pieces;
			bool periodic = false;

			for (int i = 0; i < pieces.Length; ++i)
			{
				FunctionPolynomialPiece piece = pieces[i];
				
				if (piece.Begin < 0 || piece.End > 1)
				{
					// These are for making the polynomial periodic
					periodic = true;
					
					if (piece.Begin > 0 && piece.Begin < 1)
					{
						AddDataPoint(piece.Begin, piece.Coefficients[piece.Coefficients.Length - 1]);
					}
				}
				else
				{
					AddDataPoint(piece.Begin, piece.Coefficients[piece.Coefficients.Length - 1]);
				}
			}
			
			if (!periodic && pieces.Length != 0)
			{
				FunctionPolynomialPiece last = pieces[pieces.Length - 1];
				AddDataPoint(last.End, SumCoefficients(last.Coefficients));
			}
			
			d_periodic.Active = periodic;
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
			TreeRowReference[] refs = new TreeRowReference[paths.Length];
			
			for (int i = 0; i < paths.Length; ++i)
			{
				refs[i] = new TreeRowReference(d_store, paths[i]);
			}
			
			foreach (TreeRowReference path in refs)
			{
				TreeIter piter;
				
				d_store.GetIter(out piter, path.Path);
				d_store.Remove(ref piter);
			}
		}
		
		private void AddDataPoint(double x, double y)
		{
			TreeIter piter = d_store.AppendValues(x, y);
			d_treeview.Selection.SelectIter(piter);
		}
		
		private void DoAdd(object sender, EventArgs args)
		{
			// Add new data point
			AddDataPoint(0, 0);
		}

		private void DoEdited(int idx, string newText)
		{
			TreePath[] paths = d_treeview.Selection.GetSelectedRows();
			
			if (paths.Length != 1)
			{
				return;
			}
			
			TreeIter piter;
			d_store.GetIter(out piter, paths[0]);
			
			d_store.SetValue(piter, idx, Double.Parse(newText));
		}
		
		private void Update()
		{
			List<double> x = new List<double>();
			List<double> y = new List<double>();
			
			d_store.Foreach(delegate (TreeModel model, TreePath path, TreeIter piter) {
				x.Add((double)model.GetValue(piter, 0));
				y.Add((double)model.GetValue(piter, 1));
				return false;
			});
			
			if (d_periodic.Active)
			{
				// Add virtual points to make it periodic
				double[] ptx = new double[] {x[0], x[1], x[x.Count - 1], x[x.Count - 2]};
				double[] pty = new double[] {y[0], y[1], y[y.Count - 1], y[y.Count - 2]};

				// Add points before
				x.Insert(0, -1 + ptx[2]);
				x.Insert(0, -1 + ptx[3]);
				
				y.Insert(0, pty[2]);
				y.Insert(0, pty[3]);
				
				// Add points after
				x.Add(1 + ptx[0]);
				x.Add(1 + ptx[1]);
				
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
			
			for (int i = 2; i < d_preview.Allocation.Width - 2; ++i)
			{
				int idx = i - 2;

				xx[idx] = i;
				pts[idx] = interp.Evaluate(idx / (double)xx.Length);
				
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
			ctx.Translate(0.5, Math.Ceiling(maxpt * -scale) + 4.5);

			ctx.SetSourceRGB(colors[0, 0], colors[0, 1], colors[0, 2]);
			
			int interpIdx = 0;
			int colorIdx = 0;

			while (interpIdx < interp.Pieces.Count && interp.Pieces[interpIdx].End < 0)
			{
				++interpIdx;
			}
			
			double scaleX = d_preview.Allocation.Width - 4;
			ctx.LineWidth = 2;
			
			for (int i = 0; i < xx.Length; ++i)
			{
				if (interpIdx < interp.Pieces.Count && xx[i] / scaleX > interp.Pieces[interpIdx].End)
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
			
			foreach (Interpolators.Interpolation.Piece piece in interp.Pieces)
			{
				if (piece.Begin >= 0 && piece.Begin <= 1)
				{
					ctx.Arc(piece.Begin * scaleX + 2, piece.Coefficients[piece.Coefficients.Length - 1] * scale, 3, 0, System.Math.PI * 2);
					
					ctx.SetSourceRGB(1, 1, 1);
					ctx.FillPreserve();

					ctx.SetSourceRGB(0, 0, 0);
					ctx.Stroke();
				}
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
