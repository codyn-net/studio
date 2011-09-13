using System;
using Gtk;
using System.Collections.Generic;
using System.Linq;

namespace Cpg.Studio.Widgets.Editors
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.KP_Subtract, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding"),
	 Gtk.Binding(Gdk.Key.KP_Add, "HandleAddBinding")]
	public class PiecewisePolynomial : HPaned
	{
		private class Node : Widgets.Node
		{
			public enum Column
			{
				Begin,
				End,
				Coefficients,
				Editable
			}

			private Cpg.FunctionPolynomialPiece d_piece;
			
			public Node() : this(null)
			{
			}
			
			public Node(Cpg.FunctionPolynomialPiece piece)
			{
				d_piece = piece;
				
				if (d_piece != null)
				{
					d_piece.AddNotification("coefficients", OnChanged);
					d_piece.AddNotification("begin", OnChanged);
					d_piece.AddNotification("end", OnChanged);
				}
			}
			
			public override void Dispose()
			{
				if (d_piece != null)
				{
					d_piece.RemoveNotification("coefficients", OnChanged);
					d_piece.RemoveNotification("begin", OnChanged);
					d_piece.RemoveNotification("end", OnChanged);
				}

				base.Dispose();
			}
			
			private void OnChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[NodeColumn(Column.Begin)]
			public string Begin
			{
				get { return d_piece != null ? d_piece.Begin.ToString() : "Add..."; }
			}
			
			[NodeColumn(Column.End)]
			public string End
			{
				get { return d_piece != null ? d_piece.End.ToString() : null; }
			}
			
			[NodeColumn(Column.Coefficients)]
			public string Coefficients
			{
				get
				{
					if (d_piece == null)
					{
						return null;
					}

					return String.Join(", ", Array.ConvertAll<double, string>(d_piece.Coefficients, a => a.ToString()));
				}
			}
			
			[PrimaryKey]
			public Cpg.FunctionPolynomialPiece Piece
			{
				get
				{
					return d_piece;
				}
			}
			
			[NodeColumn(Column.Editable)]
			public bool Editable
			{
				get
				{
					return d_piece != null;
				}
			}
		}

		private Wrappers.FunctionPolynomial d_function;
		private Actions d_actions;
		private TreeView<Node> d_treeview;
		private Node d_dummy;
		private bool d_select;
		private Entry d_editingEntry;
		private string d_editingPath;
		private Plot.Widget d_graph;
		private bool d_configured;
		private Entry d_period;
		private HBox d_periodWidget;
		private List<Plot.Renderers.Line> d_previewLines;
		private bool d_iscubic;
		private Biorob.Math.Point d_lastAddedData;
		private bool d_ignoreUpdatePreview;
		
		public delegate void ErrorHandler(object source, Exception exception);
		public event ErrorHandler Error = delegate {};

		public PiecewisePolynomial(Wrappers.FunctionPolynomial function, Actions actions)
		{
			d_function = function;
			d_actions = actions;
			d_previewLines = new List<Plot.Renderers.Line>();
			
			Build();
		}

		public Wrappers.FunctionPolynomial WrappedObject
		{
			get
			{
				return d_function;
			}
		}

		private void Build()
		{
			if (d_function == null)
			{
				return;
			}
			
			d_treeview = new TreeView<Node>();
			d_treeview.Show();

			d_treeview.EnableSearch = false;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			d_treeview.ShowExpanders = false;
			
			d_treeview.ButtonPressEvent += OnTreeViewButtonPressEvent;
			d_treeview.KeyPressEvent += OnTreeViewKeyPressEvent;
			
			CellRenderer renderer;
			TreeViewColumn column;
			
			renderer = new CellRendererText();
			column = new TreeViewColumn("Begin", renderer, "text", Node.Column.Begin);
			d_treeview.AppendColumn(column);
			column.MinWidth = 75;
			
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer rend, TreeModel model, TreeIter iter)  {
				Node node = d_treeview.NodeStore.GetFromIter(iter);
				CellRendererText text = rend as CellRendererText;
				
				if (node.Piece == null)
				{
					text.Style = Pango.Style.Italic;
					text.ForegroundGdk = d_treeview.Style.Foreground(StateType.Insensitive);
				}
				else
				{
					text.Style = Pango.Style.Normal;
					text.ForegroundGdk = d_treeview.Style.Foreground(d_treeview.State);
				}
			});
			
			CellRendererText rname = renderer as CellRendererText;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				d_editingEntry = args.Editable as Entry;
				d_editingPath = args.Path;
				
				Node node = d_treeview.NodeStore.FindPath(new TreePath(args.Path));
				
				if (node.Piece == null)
				{
					d_editingEntry.Text = "";
				}
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
				{
					OnEntryKeyPressed(a, rname, BeginEdited);
				};
			};

			renderer.EditingCanceled += delegate(object sender, EventArgs e) {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					BeginEdited(d_editingEntry.Text, d_editingPath);
				}
			};
			
			rname.Edited += DoBeginEdited;
			rname.Editable = true;

			renderer = new CellRendererText();
			column = new TreeViewColumn("End", renderer, "text", Node.Column.End, "editable", Node.Column.Editable);
			d_treeview.AppendColumn(column);
			column.MinWidth = 75;
			
			CellRendererText rrend = renderer as CellRendererText;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				d_editingEntry = args.Editable as Entry;
				d_editingPath = args.Path;
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
				{
					OnEntryKeyPressed(a, rrend, EndEdited);
				};
			};

			renderer.EditingCanceled += delegate(object sender, EventArgs e) {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					EndEdited(d_editingEntry.Text, d_editingPath);
				}
			};
			
			rrend.Edited += DoEndEdited;
			
			renderer = new CellRendererText();
			column = new TreeViewColumn("Coefficients", renderer, "text", Node.Column.Coefficients, "editable", Node.Column.Editable);
			d_treeview.AppendColumn(column);
			
			CellRendererText rcoef = renderer as CellRendererText;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				d_editingEntry = args.Editable as Entry;
				d_editingPath = args.Path;
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
				{
					OnEntryKeyPressed(a, rcoef, CoefficientsEdited);
				};
			};

			renderer.EditingCanceled += delegate(object sender, EventArgs e) {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					CoefficientsEdited(d_editingEntry.Text, d_editingPath);
				}
			};
			
			rcoef.Edited += DoCoefficientsEdited;
			
			ScrolledWindow sw = new ScrolledWindow();
			sw.Show();
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.ShadowType = ShadowType.EtchedIn;
			sw.Add(d_treeview);
			
			Pack1(sw, true, true);
			
			VBox vbox = new VBox(false, 3);
			vbox.Show();
			
			Frame frame = new Frame();
			frame.Shadow = ShadowType.EtchedIn;
			frame.Show();
			
			d_graph = new Plot.Widget();
			d_graph.Graph.AutoRecolor = false;
			d_graph.Graph.XAxisMode = Plot.AxisMode.Fixed;
			d_graph.Graph.YAxisMode = Plot.AxisMode.Fixed;
			d_graph.Graph.SnapRulerToData = false;
			d_graph.Graph.RulerTracksData = false;
			d_graph.Graph.SnapRulerToAxis = true;
			d_graph.Show();
			
			d_graph.ButtonPressEvent += OnGraphButtonPress;
			frame.Add(d_graph);
			
			vbox.PackStart(frame, true, true, 0);
			
			d_periodWidget = new HBox(false, 6);
			d_periodWidget.Show();
			
			d_period = new Entry();
			d_period.Show();
			d_period.Text = "1";
			d_period.WidthChars = 5;
			d_period.FocusOutEvent += OnPeriodFocusOut;
			d_period.Activated += OnPeriodActivated;
			d_period.TooltipText = "If specified, makes the function periodic on the range 0:<value>";

			d_periodWidget.PackEnd(d_period, false, false, 0);
			
			Label lbl = new Label("Period:");
			lbl.Show();
			d_periodWidget.PackEnd(lbl, false, false, 0);
			
			Pack2(vbox, true, true);
			
			Populate();
		}

		private void OnPeriodActivated(object sender, EventArgs e)
		{
			UpdatePieces();
		}

		private void OnPeriodFocusOut(object o, FocusOutEventArgs args)
		{
			UpdatePieces();
		}
		
		private bool ValidPeriod
		{
			get
			{
				try
				{
					double period = double.Parse(d_period.Text);
					
					return period > 0;
				}
				catch
				{
					return false;
				}
			}
		}

		public Widget PeriodWidget
		{
			get { return d_periodWidget; }
		}
		
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);
			
			if (!d_configured)
			{
				Position = Allocation.Width / 2;
			}
			
			d_configured = true;
		}
		
		public override void Destroy()
		{
			Disconnect();

			base.Destroy();
		}
		
		private void Populate()
		{
			d_treeview.NodeStore.Clear();
			
			if (d_function == null)
			{
				return;
			}
			
			foreach (Cpg.FunctionPolynomialPiece piece in d_function.Pieces)
			{
				d_treeview.NodeStore.Add(new Node(piece));
			}
			
			d_dummy = new Node();
			d_treeview.NodeStore.Add(d_dummy);
			
			Connect();
			
			UpdatePreview();
		}
		
		private void Connect()
		{
			if (d_function == null)
			{
				return;
			}
			
			d_function.WrappedObject.PieceAdded += OnPieceAdded;
			d_function.WrappedObject.PieceRemoved += OnPieceRemoved;
		}
		
		private void Disconnect()
		{
			if (d_function == null)
			{
				return;
			}
			
			d_function.WrappedObject.PieceAdded -= OnPieceAdded;
			d_function.WrappedObject.PieceRemoved -= OnPieceRemoved;
		}
		
		private void OnPieceAdded(object source, Cpg.PieceAddedArgs args)
		{
			Node node = new Node(args.Piece);

			d_treeview.NodeStore.Remove(d_dummy);
			d_treeview.NodeStore.Add(node);
			d_treeview.NodeStore.Add(d_dummy);
			
			if (d_select)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectPath(node.Path);

				d_treeview.SetCursor(node.Path, d_treeview.GetColumn(0), true);
			}
			
			UpdatePreview();
		}
		
		private void OnPieceRemoved(object source, Cpg.PieceRemovedArgs args)
		{
			d_treeview.NodeStore.Remove(args.Piece);
			d_lastAddedData = null;
			
			UpdatePreview();
		}
		
		private bool PieceMatch(Biorob.Math.Functions.PiecewisePolynomial.Piece p1,
		                        Biorob.Math.Functions.PiecewisePolynomial.Piece p2)
		{
			if (p1.Coefficients.Length != p2.Coefficients.Length)
			{
				return false;
			}
			
			for (int i = 0; i < p1.Coefficients.Length; ++i)
			{
				if (System.Math.Abs(p1.Coefficients[i] - p2.Coefficients[i]) > Biorob.Math.Constants.Epsilon)
				{
					return false;
				}
			}
			
			return true;
		}
		
		private Biorob.Math.Range DeterminePeriod()
		{
			List<Biorob.Math.Functions.PiecewisePolynomial.Piece> pieces;
			pieces = new List<Biorob.Math.Functions.PiecewisePolynomial.Piece>();
			
			foreach (Cpg.FunctionPolynomialPiece piece in d_function.Pieces)
			{
				pieces.Add(new Biorob.Math.Functions.PiecewisePolynomial.Piece(new Biorob.Math.Range(piece.Begin, piece.End), piece.Coefficients));
			}
			
			return DeterminePeriod(pieces);
		}
		
		private bool PieceRangeMatch(Biorob.Math.Functions.PiecewisePolynomial.Piece p1,
		                             Biorob.Math.Functions.PiecewisePolynomial.Piece p2,
		                             double span)
		{
			return System.Math.Abs(p2.End - p1.End - span) < Biorob.Math.Constants.Epsilon;
		}

		private Biorob.Math.Range DeterminePeriod(List<Biorob.Math.Functions.PiecewisePolynomial.Piece> pieces)
		{
			if (pieces.Count < 6)
			{
				return null;
			}
			
			// It's periodic when the first piece matches the second to last
			// and the second piece matches the last
			int num = pieces.Count;

			if (!PieceMatch(pieces[1], pieces[num - 2]))
			{
				return null;
			}
			
			if (pieces[1].Begin > 0 || pieces[1].End <= 0)
			{
				return null;
			}
			
			// So it's periodic, determine the period!
			double span = pieces[num - 2].Begin - pieces[1].Begin;
			
			if (!PieceRangeMatch(pieces[0], pieces[num - 3], span) ||
			    !PieceRangeMatch(pieces[2], pieces[num - 1], span))
			{
				return null;
			}

			return new Biorob.Math.Range(0, span);
		}
		
		private void UpdatePreview()
		{
			if (d_ignoreUpdatePreview)
			{
				return;
			}

			foreach (Plot.Renderers.Line line in d_previewLines)
			{
				d_graph.Graph.Remove(line);
			}
			
			d_previewLines.Clear();
			
			d_iscubic = true;
			List<Biorob.Math.Functions.PiecewisePolynomial.Piece> pieces = new List<Biorob.Math.Functions.PiecewisePolynomial.Piece>();
			
			// Determine if we are going to draw cubics or just sampled
			foreach (Cpg.FunctionPolynomialPiece piece in d_function.Pieces)
			{
				if (piece.Coefficients.Length != 4)
				{
					d_iscubic = false;
				}
				
				pieces.Add(new Biorob.Math.Functions.PiecewisePolynomial.Piece(new Biorob.Math.Range(piece.Begin, piece.End), piece.Coefficients));
			}
			
			Biorob.Math.Range period = DeterminePeriod(pieces);
			
			if (period != null)
			{
				d_period.Text = (period.Max - period.Min).ToString();
			}
			else
			{
				d_period.Text = "";
			}
			
			Biorob.Math.Functions.PiecewisePolynomial poly;
			poly = new Biorob.Math.Functions.PiecewisePolynomial(pieces);
			
			double msize = 8;
			Plot.Renderers.Line.MarkerType mtype = Plot.Renderers.Line.MarkerType.FilledCircle;
			
			if (poly.Count == 0 && d_lastAddedData != null)
			{
				List<Biorob.Math.Point> data = new List<Biorob.Math.Point>();
				data.Add(d_lastAddedData);

				Plot.Renderers.Line line = new Plot.Renderers.Line(data, d_graph.Graph.ColorMap[0], "preview");
				line.MarkerSize = msize;
				line.MarkerStyle = mtype;
				
				d_graph.Graph.Add(line);
				d_previewLines.Add(line);
			}
			else if (d_iscubic && poly.Count != 0)
			{
				// If it's cubic, then use the bezier line
				Plot.Renderers.Bezier bezier = new Plot.Renderers.Bezier(poly, d_graph.Graph.ColorMap[0], "preview");

				bezier.Periodic = period;
				bezier.MarkerSize = msize;
				bezier.MarkerStyle = mtype;
				
				d_graph.Graph.Add(bezier);
				d_previewLines.Add(bezier);
			}
			else if (poly.Count != 0)
			{
				// Otherwise use two lines, one with markers, the other sampled
				Plot.Renderers.Line line = new Plot.Renderers.Line("preview");
				line.Color = d_graph.Graph.ColorMap[0];				
				
				if (pieces.Count > 0)
				{
					line.GenerateData(new Biorob.Math.Range(pieces[0].Begin, pieces[pieces.Count - 1].End),
					                  1000,
					                  x => new Biorob.Math.Point(x, poly.Evaluate(x)));
				}
				
				d_graph.Graph.Add(line);
				
				Plot.Renderers.Line markers = new Plot.Renderers.Line();
				markers.Color = line.Color;
				markers.MarkerSize = msize;
				markers.MarkerStyle = mtype;
				
				List<Biorob.Math.Point> data = new List<Biorob.Math.Point>();
				
				foreach (Biorob.Math.Functions.PiecewisePolynomial.Piece piece in pieces)
				{
					data.Add(new Biorob.Math.Point(piece.Begin, piece.Coefficients[piece.Coefficients.Length - 1]));
				}
				
				if (pieces.Count > 0)
				{
					Biorob.Math.Functions.PiecewisePolynomial.Piece piece = pieces[pieces.Count - 1];
					data.Add(new Biorob.Math.Point(piece.End, piece.Coefficients.Sum()));
				}

				d_graph.Graph.Add(markers);
				
				d_previewLines.Add(line);
				d_previewLines.Add(markers);
			}
		}
		
		private void DoAddPiece()
		{
			FunctionPolynomialPiece piece;

			if (d_function.Pieces.Length == 0)
			{
				piece = new FunctionPolynomialPiece(0, 1, 0, 0, 0, 1);
			}
			else
			{
				FunctionPolynomialPiece last = d_function.Pieces[d_function.Pieces.Length - 1];
				piece = new FunctionPolynomialPiece(last.End, last.End + (last.End - last.Begin), last.Coefficients);
			}

			d_select = true;
			d_actions.Do(new Undo.AddFunctionPolynomialPiece(d_function, piece));
			d_select = false;
		}
		
		private void DoRemovePiece()
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				Node node = d_treeview.NodeStore.FindPath(path);
				
				if (node == d_dummy)
				{
					continue;
				}
				
				actions.Add(new Undo.RemoveFunctionPolynomialPiece(d_function, node.Piece));
			}

			try
			{
				d_actions.Do(new Undo.Group(actions));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
			}
		}
		
		private delegate void EditedHandler(string text, string path);
		
		private void OnEntryKeyPressed(KeyPressEventArgs args, CellRenderer renderer, EditedHandler handler)
		{
			if (args.Event.Key == Gdk.Key.Tab ||
			    args.Event.Key == Gdk.Key.ISO_Left_Tab ||
			    args.Event.Key == Gdk.Key.KP_Tab)
			{
				TreeViewColumn column;

				/* Start editing the next cell */
				CellRenderer next = NextCell(renderer, (args.Event.State & Gdk.ModifierType.ShiftMask) != 0, out column);
				
				if (next != null)
				{
					handler(d_editingEntry.Text, d_editingPath);
					renderer.StopEditing(false);
					
					if (next is CellRendererToggle || next is CellRendererCombo)
					{
						d_treeview.SetCursorOnCell(new TreePath(d_editingPath), column, next, false);
					}
					else
					{
						d_treeview.SetCursorOnCell(new TreePath(d_editingPath), column, next, true);
					}
					
					args.RetVal = true;
				}
				else
				{
					d_treeview.GrabFocus();
					args.RetVal = false;
				}
			}
			else
			{
				args.RetVal = false;
			}
		}
		
		private TreeViewColumn NextColumn(TreePath path, TreeViewColumn column, bool prev, out TreePath next)
		{
			TreeViewColumn[] columns = d_treeview.Columns;
			
			int idx = Array.IndexOf(columns, column);
			next = null;
			
			if (idx < 0)
			{
				return null;
			}
			
			next = path.Copy();
			
			if (!prev && idx == columns.Length - 1)
			{
				next.Next();
				idx = 0;
			}
			else if (prev && idx == 0)
			{
				if (!next.Prev())
				{
					return null;
				}
				
				idx = columns.Length - 1;
			}
			else if (!prev)
			{
				++idx;
			}
			else
			{
				--idx;
			}
			
			return columns[idx];
		}

		private void OnTreeViewKeyPressEvent(object o, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Tab ||
			    args.Event.Key == Gdk.Key.KP_Tab ||
			    args.Event.Key == Gdk.Key.ISO_Left_Tab)
			{
				TreePath path = null;
				TreeViewColumn column;
				TreePath next;
				
				d_treeview.GetCursor(out path, out column);
				
				if (path == null)
				{
					args.RetVal = false;
					return;
				}
				
				column = NextColumn(path, column, (args.Event.State & Gdk.ModifierType.ShiftMask) != 0, out next);
				
				if (column != null)
				{
					CellRenderer r = column.CellRenderers[0];
					d_treeview.SetCursor(next, column, r is CellRendererText && !(r is CellRendererCombo));
					args.RetVal = true;
				}
				else
				{
					args.RetVal = false;
				}
			}
			else
			{
				args.RetVal = false;
			}
		}
		
		private CellRenderer NextCell(CellRenderer renderer, bool prev, out TreeViewColumn column)
		{
			TreeViewColumn[] columns = d_treeview.Columns;
			bool getnext = false;
			CellRenderer prevedit = null;
			column = null;

			for (int j = 0; j < columns.Length; ++j)
			{
				CellRenderer[] renderers = columns[j].CellRenderers;

				for (int i = 0; i < renderers.Length; ++i)
				{
					if (renderer == renderers[i])
					{
						getnext = true;
						
						if (prev)
						{
							return prevedit;
						}
					}
					else if (getnext)
					{
						column = columns[j];
						return renderers[i];
					}
					else
					{
						prevedit = renderers[i];
						column = columns[j];
					}
				}
			}
			
			column = null;
			return null;
		}
		
		private void ShowPopup(Gdk.EventButton evnt)
		{
			Gtk.AccelGroup grp = new Gtk.AccelGroup();
			
			Gtk.Menu menu = new Gtk.Menu();
			menu.Show();
			menu.AccelGroup = grp;
			
			MenuItem item;
			
			item = new MenuItem("Add");
			item.AccelPath = "<CpgStudio>/Widgets/Editors/PiecewisePolynomial/Add";
			
			AccelMap.AddEntry("<CpgStudio>/Widgets/Editors/PiecewisePolynomial/Add", (uint)Gdk.Key.KP_Add, Gdk.ModifierType.None);

			item.Show();
			item.Activated += DoAddPiece;
			
			menu.Append(item);

			item = new MenuItem("Remove");
			item.AccelPath = "<CpgStudio>/Widgets/Editors/PiecewisePolynomial/Remove";
			item.Show();
			
			AccelMap.AddEntry("<CpgStudio>/Widgets/Editors/PiecewisePolynomial/Remove", (uint)Gdk.Key.KP_Subtract, Gdk.ModifierType.None);
			
			item.Sensitive = (d_treeview.Selection.CountSelectedRows() > 0);
			item.Activated += DoRemovePiece;
			
			menu.Append(item);
				
			menu.Popup(null, null, null, evnt.Button, evnt.Time);
		}
		
		private void HandleAddBinding()
		{
			DoAddPiece();
		}
		
		private void HandleDeleteBinding()
		{
			DoRemovePiece();
		}
		
		private void DoAddPiece(object source, EventArgs args)
		{
			DoAddPiece();
		}
		
		private void DoRemovePiece(object source, EventArgs args)
		{
			DoRemovePiece();
		}
		
		[GLib.ConnectBefore]
		private void OnTreeViewButtonPressEvent(object source, ButtonPressEventArgs args)
		{
			if (args.Event.Type == Gdk.EventType.ButtonPress &&
			    args.Event.Button == 3)
			{
				ShowPopup(args.Event);
				args.RetVal = true;
				return;
			}
			
			TreePath path;
			
			if (args.Event.Type == Gdk.EventType.ButtonPress &&
			    args.Event.Button == 1 &&
			    args.Event.Window == d_treeview.BinWindow)
			{
				if (d_treeview.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path))
				{			
					Node node = d_treeview.NodeStore.FindPath(path);
			
					if (node == d_dummy)
					{
						/* Start editing the dummy node */
						d_treeview.SetCursor(path, d_treeview.Columns[0], true);
						args.RetVal = true;
						return;
					}
				}
			}

			if (args.Event.Type != Gdk.EventType.TwoButtonPress &&
			    args.Event.Type != Gdk.EventType.ThreeButtonPress)
			{
				return;
			}
			
			if (args.Event.Window != d_treeview.BinWindow)
			{
				return;
			}
			
			TreeViewColumn column;
			TreeView tv = (TreeView)source;
			
			if (!tv.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path, out column))
			{
				return;
			}
			
			tv.GrabFocus();
			tv.Selection.SelectPath(path);
			tv.SetCursor(path, column, true);

			args.RetVal = true;
		}
		
		private void BeginEdited(string newBegin, string path)
		{
			if (String.IsNullOrEmpty(newBegin.Trim()))
			{
				return;
			}
			
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			double val = 0;
			
			try
			{
				val = double.Parse(newBegin.Trim());
			}
			catch (Exception e)
			{
				Error(this, e);
				return;
			}
			
			if (node.Piece == null)
			{
				/* Add a new piece */
				try
				{
					FunctionPolynomialPiece piece;
					
					if (d_function.Pieces.Length == 0)
					{
						piece = new FunctionPolynomialPiece(val, val + 1, 0, 0, 0, 1);
					}
					else
					{
						FunctionPolynomialPiece last = d_function.Pieces[d_function.Pieces.Length - 1];
						piece = new FunctionPolynomialPiece(val, val + (last.End - last.Begin), 0, 0, 0, 1);
					}

					d_actions.Do(new Undo.AddFunctionPolynomialPiece(d_function, piece));
				}
				catch (GLib.GException err)
				{
					// Display could not remove, or something
					Error(this, err);
				}
				
				return;
			}
			
			if (val == node.Piece.Begin)
			{
				return;
			}

			try
			{
				d_actions.Do(new Undo.ModifyFunctionPolynomialPieceBegin(d_function, node.Piece, val));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
		}
		
		private void DoBeginEdited(object source, EditedArgs args)
		{
			BeginEdited(args.NewText, args.Path);
		}
		
		private void EndEdited(string newEnd, string path)
		{
			if (String.IsNullOrEmpty(newEnd.Trim()))
			{
				return;
			}
			
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			double val = 0;
			
			try
			{
				val = double.Parse(newEnd.Trim());
			}
			catch (Exception e)
			{
				Error(this, e);
				return;
			}
			
			if (node.Piece == null)
			{
				return;
			}
			
			if (val == node.Piece.End)
			{
				return;
			}

			try
			{
				d_actions.Do(new Undo.ModifyFunctionPolynomialPieceEnd(d_function, node.Piece, val));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
		}
		
		private void DoEndEdited(object source, EditedArgs args)
		{
			EndEdited(args.NewText, args.Path);
		}
		
		private void CoefficientsEdited(string newCoef, string path)
		{
			if (String.IsNullOrEmpty(newCoef.Trim()))
			{
				return;
			}
			
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null || node.Piece == null)
			{
				return;
			}
			
			double[] coefs;
			
			try
			{
				coefs = Array.ConvertAll<string, double>(newCoef.Split(','), a => double.Parse(a.Trim()));
			}
			catch (Exception e)
			{
				Error(this, e);
				return;
			}
			
			if (coefs.Length == node.Piece.Coefficients.Length)
			{
				bool same = true;

				for (int i = 0; i < coefs.Length; ++i)
				{
					if (coefs[i] != node.Piece.Coefficients[i])
					{
						same = false;
						break;
					}
				}
				
				if (same)
				{
					return;
				}
			}

			try
			{
				d_actions.Do(new Undo.ModifyFunctionPolynomialPieceCoefficients(d_function, node.Piece, coefs));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
		}
		
		private void DoCoefficientsEdited(object source, EditedArgs args)
		{
			CoefficientsEdited(args.NewText, args.Path);
		}
		
		private double Period
		{
			get
			{
				try
				{
					return double.Parse(d_period.Text);
				}
				catch
				{
					return 0;
				}
			}
		}
		
		private List<Cpg.FunctionPolynomialPiece> NonPeriodicPieces()
		{
			List<Cpg.FunctionPolynomialPiece> ret = new List<Cpg.FunctionPolynomialPiece>();
			double period = 0;
			bool hasperiod = ValidPeriod;
			
			if (hasperiod)
			{
				period = Period;
			}
			
			foreach (Cpg.FunctionPolynomialPiece piece in d_function.Pieces)
			{
				if (hasperiod && (piece.Begin < 0 || piece.End > period))
				{
					continue;
				}
				
				ret.Add(piece);
			}
			
			return ret;
		}
		
		[GLib.ConnectBefore]
		private void OnGraphButtonPress(object source, ButtonPressEventArgs args)
		{
			if (!d_iscubic || args.Event.Type != Gdk.EventType.TwoButtonPress || args.Event.Button != 1)
			{
				return;
			}
			
			Biorob.Math.Point pt = d_graph.Graph.PixelToAxis(new Biorob.Math.Point(args.Event.X, args.Event.Y));
			
			List<Biorob.Math.Point> added = new List<Biorob.Math.Point>();
			
			if (d_graph.Graph.SnapRulerToAxis)
			{
				pt = d_graph.Graph.SnapToAxis(pt, d_graph.Graph.SnapRulerToAxisFactor);
			}

			added.Add(pt);
			
			if (d_function.Pieces.Length == 0 && d_lastAddedData != null)
			{
				added.Add(d_lastAddedData);
			}
			
			d_lastAddedData = pt;
			
			UpdatePieces(added.ToArray());
			args.RetVal = true;
		} 
		
		private List<Undo.IAction> RemovePeriodicActions()
		{
			// Remove pieces which were generated from the period
			Biorob.Math.Range range = DeterminePeriod();
			
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			if (range == null)
			{
				return actions;
			}
			
			foreach (Cpg.FunctionPolynomialPiece piece in d_function.Pieces)
			{
				if (piece.Begin < range.Min || piece.End > range.Max)
				{
					actions.Add(new Undo.RemoveFunctionPolynomialPiece(d_function, piece));
				}
			}
			
			return actions;
		}
		
		private void RemovePeriodic()
		{
			List<Undo.IAction> actions = RemovePeriodicActions();
			
			if (actions.Count != 0)
			{
				try
				{
					d_actions.Do(new Undo.Group(actions));
				}
				catch (Exception e)
				{
					Error(this, e);
				}
			}
		}
		
		private void UpdatePieces(params Biorob.Math.Point[] added)
		{
			d_ignoreUpdatePreview = true;

			bool hasperiod = ValidPeriod;

			// There is a new period, do it
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			Biorob.Math.Range range = DeterminePeriod();
			List<Biorob.Math.Point> data = new List<Biorob.Math.Point>();
			Cpg.FunctionPolynomialPiece last = null;
			
			double period = 0;
			
			if (hasperiod)
			{
				period = Period;
			}
			
			foreach (Cpg.FunctionPolynomialPiece piece in d_function.Pieces)
			{
				actions.Add(new Undo.RemoveFunctionPolynomialPiece(d_function, piece));

				if ((!hasperiod || (piece.Begin >= 0 && piece.End <= period)) && (range == null || (piece.Begin >= range.Min && piece.End <= range.Max)))
				{
					data.Add(new Biorob.Math.Point(piece.Begin, piece.Coefficients[piece.Coefficients.Length - 1]));
					last = piece;
				}
			}
			
			if (last != null)
			{
				data.Add(new Biorob.Math.Point(last.End, last.Coefficients.Sum()));
			}
			
			foreach (Biorob.Math.Point pt in added)
			{
				if (!hasperiod || (pt.X >= 0 && pt.X <= period))
				{
					data.Add(pt);
				}
			}
			
			// Then make it periodic
			Biorob.Math.Interpolation.PChip pchip = new Biorob.Math.Interpolation.PChip();
			data.Sort();
			
			if (hasperiod)
			{
				Biorob.Math.Interpolation.Periodic.Extend(data, 0, period);
			}

			Biorob.Math.Functions.PiecewisePolynomial poly = pchip.InterpolateSorted(data);
			
			foreach (Biorob.Math.Functions.PiecewisePolynomial.Piece piece in poly.Pieces)
			{
				Cpg.FunctionPolynomialPiece p;
				
				p = new FunctionPolynomialPiece(piece.Begin, piece.End, piece.Coefficients);
				actions.Add(new Undo.AddFunctionPolynomialPiece(d_function, p));
			}
			
			try
			{
				d_actions.Do(new Undo.Group(actions));
			}
			catch (Exception e)
			{
				Error(this, e);
			}
			
			d_ignoreUpdatePreview = false;
			
			UpdatePreview();
		}
	}
}

