using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class PolynomialsView : FunctionsHelper<FunctionPolynomialNode, Wrappers.FunctionPolynomial>
	{
		private HPaned d_paned;
		private Button d_removeButton;
		private Button d_pieceRemoveButton;
		private FunctionPolynomialNode d_selected;
		
		private NodeStore<FunctionPolynomialPieceNode> d_store;
		private TreeView d_treeview;
		private bool d_selectPiece;
		
		public PolynomialsView(Actions actions, Wrappers.Network network) : base(actions, network)
		{
			InitUi();
		}
		
		private void InitFunctionsUi()
		{
			TreeViewColumn column;
			CellRendererText renderer;
			
			// Name column
			renderer = new CellRendererText();
			column = new TreeViewColumn("Name", renderer, "text", 0);

			renderer.Editable = true;
			renderer.Edited += DoNameEdited;

			column.Resizable = true;
			column.MinWidth = 75;

			TreeView.AppendColumn(column);
			
			ScrolledWindow vw = new Widgets.ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(TreeView);

			TreeView.Show();
			vw.Show();

			VBox box = new VBox(false, 3);
			box.Show();
			
			box.PackStart(vw);

			HBox hbox = new HBox(false, 3);
			hbox.Show();
			
			Alignment align = new Alignment(0, 0, 1, 1);
			align.SetPadding(0, 0, 6, 0);
			align.Show();
			
			align.Add(hbox);
			
			box.PackStart(align, false, false, 0);

			d_removeButton = new Button();
			d_removeButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeButton.Sensitive = false;
			d_removeButton.Clicked += delegate {
				RemoveSelection();	
			};
			d_removeButton.ShowAll();

			hbox.PackStart(d_removeButton, false, false ,0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAdd;
			but.ShowAll();

			hbox.PackStart(but, false, false, 0);

			TreeView.Selection.Changed += DoSelectionChanged;
			
			NodeStore.Bind(TreeView);
			
			d_paned.Add1(box);
		}
		
		private void InitPiecesUi()
		{
			d_store = new NodeStore<FunctionPolynomialPieceNode>();
			d_store.SortColumn = 0;

			d_treeview = new TreeView(new TreeModelAdapter(d_store));
			d_treeview.Show();

			d_treeview.HeadersVisible = true;
			d_treeview.ShowExpanders = false;
			d_treeview.RulesHint = true;
			
			ScrolledWindow vw = new Widgets.ScrolledWindow();
			vw.Show();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(d_treeview);
			
			CellRendererText renderer;
			TreeViewColumn column;
			
			// Begin column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoBeginEdited;

			column = new TreeViewColumn("Begin", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 75;

			d_treeview.AppendColumn(column);
			
			// End column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoEndEdited;

			column = new TreeViewColumn("End", renderer, "text", 1);
			column.Resizable = true;
			column.MinWidth = 75;

			d_treeview.AppendColumn(column);
			
			// Coefficients column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoCoefficientsEdited;

			column = new TreeViewColumn("Coefficients", renderer, "text", 2);
			column.Resizable = true;
			column.MinWidth = 200;

			d_treeview.AppendColumn(column);
			
			VBox vbox = new VBox(false, 3);
			vbox.Show();

			vbox.PackStart(vw, true, true, 0);
			
			HBox hbox = new HBox(false, 3);
			hbox.Show();
			
			vbox.PackStart(hbox, false, false, 0);

			d_treeview.KeyPressEvent += DoPieceTreeViewKeyPress;
			
			d_pieceRemoveButton = new Button();
			d_pieceRemoveButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_pieceRemoveButton.Sensitive = false;
			d_pieceRemoveButton.Clicked += DoRemovePiece;
			d_pieceRemoveButton.ShowAll();

			hbox.PackStart(d_pieceRemoveButton, false, false ,0);

			Button but;
			but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAddPiece;
			but.ShowAll();

			hbox.PackStart(but, false, false, 0);
			
			but = new Button();
			but.Add(new Label("Interpolate"));
			but.Clicked += DoInterpolate;
			but.ShowAll();
			
			hbox.PackEnd(but, false, false, 6);

			d_treeview.Selection.Changed += DoPieceSelectionChanged;
			d_paned.Add2(vbox);
		}
		
		private void InitUi()
		{
			d_paned = new HPaned();
			d_paned.Show();
			
			PackStart(d_paned, true, true, 0);
			
			InitFunctionsUi();
			InitPiecesUi();
			
			d_paned.Position = 150;
			
			InitStore();
		}
		
		private void DoAdd(object sender, EventArgs args)
		{
			int i = 1;
			string funcName;

			while (true)
			{
				funcName = String.Format("f{0}", i++);

				if (Network.GetFunction(funcName) == null)
				{
					break;
				}
			}

			Wrappers.FunctionPolynomial function = new Wrappers.FunctionPolynomial(funcName);
			Add(function);
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			FunctionPolynomialNode node = FromStorage(args.Path);

			if (args.NewText == String.Empty || node.Function.Id == args.NewText.Trim())
			{
				return;
			}

			Actions.Do(new Undo.ModifyObjectId(node.Function, args.NewText.Trim()));
		}
		
		private void SetSelected(FunctionPolynomialNode node)
		{
			if (d_selected != null)
			{
				d_selected.Function.WrappedObject.PieceAdded -= HandlePieceAdded;
				d_selected.Function.WrappedObject.PieceRemoved -= HandlePieceRemoved;
			}
			
			d_selected = node;
			
			if (d_selected != null)
			{
				d_selected.Function.WrappedObject.PieceAdded += HandlePieceAdded;
				d_selected.Function.WrappedObject.PieceRemoved += HandlePieceRemoved;
			}
		}
		
		private void HandlePieceAdded(object source, Cpg.PieceAddedArgs args)
		{
			TreeIter iter;

			d_store.Add(new FunctionPolynomialPieceNode(args.Piece), out iter);
			
			if (d_selectPiece)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
			}
		}
		
		private void HandlePieceRemoved(object source, Cpg.PieceRemovedArgs args)
		{
			d_store.Remove(args.Piece);
		}

		private void DoSelectionChanged(object source, EventArgs args)
		{
			TreeSelection selection = TreeView.Selection;
			d_removeButton.Sensitive = selection.CountSelectedRows() != 0;
			
			if (selection.CountSelectedRows() == 1)
			{
				SetSelected(FromStorage(selection.GetSelectedRows()[0]));
			}
			else
			{
				SetSelected(null);
			}

			InitStore();
		}
		
		private void InitStore()
		{
			d_store.Clear();
			
			if (d_selected == null)
			{
				d_paned.Child2.Sensitive = false;			
				return;
			}
			
			d_paned.Child2.Sensitive = true;
			d_pieceRemoveButton.Sensitive = true;
			
			foreach (Cpg.FunctionPolynomialPiece piece in d_selected.Function.Pieces)
			{
				TreeIter iter;
				d_store.Add(new FunctionPolynomialPieceNode(piece), out iter);
			}
		}
		
		// Pieces
		private void DoCoefficientsEdited(object source, EditedArgs args)
		{
			FunctionPolynomialPieceNode node = (FunctionPolynomialPieceNode)d_store.FindPath(args.Path);
			
			string[] parts = args.NewText.Split(new char[] {','});
			double[] coefs = Array.ConvertAll<string, double>(parts, item => Double.Parse(item));

			Actions.Do(new Undo.ModifyFunctionPolynomialPieceCoefficients(d_selected.Function, node.Piece, coefs));
		}
		
		private void DoBeginEdited(object source, EditedArgs args)
		{
			FunctionPolynomialPieceNode node = (FunctionPolynomialPieceNode)d_store.FindPath(args.Path);
			
			double val = Double.Parse(args.NewText);
			
			if (node.Piece.Begin != val)
			{
				Actions.Do(new Undo.ModifyFunctionPolynomialPieceBegin(d_selected.Function, node.Piece, Double.Parse(args.NewText)));
			}
		}
		
		private void DoEndEdited(object source, EditedArgs args)
		{
			FunctionPolynomialPieceNode node = (FunctionPolynomialPieceNode)d_store.FindPath(args.Path);
			
			double val = Double.Parse(args.NewText);
			
			if (node.Piece.End != val)
			{
				Actions.Do(new Undo.ModifyFunctionPolynomialPieceEnd(d_selected.Function, node.Piece, val));
			}
		}
		
		private void DoPieceSelectionChanged(object source, EventArgs args)
		{
			TreeSelection selection = (TreeSelection)source;

			d_pieceRemoveButton.Sensitive = selection.CountSelectedRows() != 0;
		}
		
		private void RemoveSelectedPieces()
		{
			TreeSelection selection = d_treeview.Selection;
			List<Cpg.FunctionPolynomialPiece> pieces = new List<Cpg.FunctionPolynomialPiece>();
			
			foreach (TreePath path in selection.GetSelectedRows())
			{
				FunctionPolynomialPieceNode node = (FunctionPolynomialPieceNode)d_store.FindPath(path);
				pieces.Add(node.Piece);
			}
			
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Cpg.FunctionPolynomialPiece piece in pieces)
			{
				actions.Add(new Undo.RemoveFunctionPolynomialPiece(d_selected.Function, piece));
			}
			
			Actions.Do(new Undo.Group(actions));
		}
		
		private void DoPieceTreeViewKeyPress(object sender, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				RemoveSelectedPieces();
			}
		}
		
		private void DoRemovePiece(object sender, EventArgs args)
		{
			RemoveSelectedPieces();
		}
		
		private void AddPiece()
		{
			FunctionPolynomialPiece[] pieces = d_selected.Function.Pieces;
			double begin = 0;
			double end = 1;
			
			if (pieces.Length != 0)
			{
				begin = pieces[pieces.Length - 1].End;
				end = begin + (pieces[pieces.Length - 1].End - pieces[pieces.Length - 1].Begin);
			}
			
			FunctionPolynomialPiece piece = new FunctionPolynomialPiece(begin, end, 0, 0, 0, 1);
			Actions.Do(new Undo.AddFunctionPolynomialPiece(d_selected.Function, piece));
		}
		
		private void DoAddPiece(object sender, EventArgs args)
		{
			d_selectPiece = true;
			AddPiece();
			d_selectPiece = false;
		}
		
		public void Select(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece)
		{
			Select(function);
			
			if (d_selected != null)
			{
				TreeIter iter;

				if (d_store.Find(piece, out iter))
				{
					d_treeview.Selection.UnselectAll();
					d_treeview.Selection.SelectIter(iter);
				}
			}
		}
		
		private void DoInterpolate(object sender, EventArgs args)
		{
			if (d_selected == null)
			{
				return;
			}

			Dialogs.Interpolate dlg = new Dialogs.Interpolate(Toplevel as Gtk.Window, d_selected.Function);
			dlg.Show();
			
			FunctionPolynomialNode func = d_selected;

			dlg.Response += delegate(object o, ResponseArgs a1) {
				if (a1.ResponseId == ResponseType.Apply)
				{
					ApplyInterpolation(func, dlg.Interpolation, dlg.Period);
				}
				
				dlg.Destroy();
			};
		}

		private void ApplyInterpolation(FunctionPolynomialNode node, Interpolators.Interpolation interpolation, double[] period)
		{
			if (interpolation == null)
			{
				return;
			}
			
			node.Function.ClearPieces();
			
			if (period != null)
			{
				node.Function.Period = new Wrappers.FunctionPolynomial.PeriodType();
				node.Function.Period.Begin = period[0];
				node.Function.Period.End = period[1];
			}
			else
			{
				node.Function.Period = null;
			}

			foreach (Interpolators.Interpolation.Piece piece in interpolation.Pieces)
			{
				if (period == null || (piece.Begin >= period[0] && piece.End <= period[1]))
				{
					node.Function.Add(new Cpg.FunctionPolynomialPiece(piece.Begin, piece.End, piece.Coefficients));
				}
			}
		}
	}
}
