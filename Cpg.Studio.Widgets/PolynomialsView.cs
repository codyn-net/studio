using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class PolynomialsView : FunctionsHelper<FunctionPolynomialNode, Wrappers.FunctionPolynomial>
	{
		//private NodeStore d_pieceStore;
		//private NodeView d_pieceTreeview;
		
		private Gtk.Button d_removeButton;
		private Gtk.Button d_pieceRemoveButton;
		
		private Gtk.HPaned d_paned;

		public PolynomialsView(Actions actions, Wrappers.Network network) : base(actions, network)
		{
			InitUi();
		}
		
		private void InitUi()
		{
			d_paned = new Gtk.HPaned();
			d_paned.Show();
			
			PackStart(d_paned, true, true, 0);
			
			// Name column
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoNameEdited;

			TreeViewColumn column = new TreeViewColumn("Name", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 75;

			TreeView.AppendColumn(column);			
			ScrolledWindow vw = new Widgets.ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(TreeView);

			TreeView.Show();
			vw.Show();

			d_paned.Add1(vw);
			
			HBox hbox = new HBox(false, 3);
			hbox.Show();
			
			Alignment align = new Alignment(0, 0, 1, 1);
			align.SetPadding(0, 0, 6, 0);
			align.Show();
			
			align.Add(hbox);
			
			PackStart(align, false, false, 0);

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
			
			/*
			// Pieces
			d_pieceStore = new PieceNodeStore();
			d_pieceTreeview = new NodeView(d_pieceStore);
			
			// Begin column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoBeginEdited;

			column = new TreeViewColumn("Begin", renderer, new object[] {"text", 0});
			column.Resizable = true;
			column.MinWidth = 75;

			d_pieceTreeview.AppendColumn(column);
			
			// End column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoEndEdited;

			column = new TreeViewColumn("End", renderer, new object[] {"text", 1});
			column.Resizable = true;
			column.MinWidth = 75;

			d_pieceTreeview.AppendColumn(column);
			
			// Coefficients column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoCoefficientsEdited;

			column = new TreeViewColumn("Coefficients", renderer, new object[] {"text", 2});
			column.Resizable = true;
			column.MinWidth = 200;

			d_pieceTreeview.AppendColumn(column);
			
			vw = new Widgets.ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(d_pieceTreeview);

			d_pieceTreeview.Show();
			vw.Show();
			
			VBox vbox = new VBox(false, 3);
			vbox.Show();
			vbox.PackStart(vw, true, true, 0);
			
			hbox = new HBox(false, 3);
			hbox.Show();
			
			vbox.PackStart(hbox, false, false, 0);

			d_pieceTreeview.KeyPressEvent += DoPieceTreeViewKeyPress;
			
			d_pieceRemoveButton = new Button();
			d_pieceRemoveButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_pieceRemoveButton.Sensitive = false;
			d_pieceRemoveButton.Clicked += DoRemovePiece;
			d_pieceRemoveButton.ShowAll();

			hbox.PackStart(d_pieceRemoveButton, false, false ,0);

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

			d_pieceTreeview.NodeSelection.Changed += DoPieceSelectionChanged;
			d_paned.Add2(vbox);
			
			d_paned.Position = 150;
			FillPieces();*/
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

		private void DoSelectionChanged(object source, EventArgs args)
		{
			d_removeButton.Sensitive = TreeView.Selection.CountSelectedRows() != 0;
			
			/*NodeSelection selection = source as NodeSelection;
			FillPieces();*/
		}
		
		private void FillPieces()
		{
			/*NodeSelection selection = d_treeview.NodeSelection;
			d_pieceStore.Clear();
			
			if (selection.SelectedNodes.Length != 1)
			{
				d_paned.Child2.Sensitive = false;			
				return;
			}
			
			PolynomialNode node = (PolynomialNode)selection.SelectedNodes[0];
			
			d_paned.Child2.Sensitive = true;
			d_pieceRemoveButton.Sensitive = d_pieceTreeview.NodeSelection.SelectedNodes.Length != 0;
			
			foreach (Cpg.FunctionPolynomialPiece piece in node.Function.Pieces)
			{
				d_pieceStore.AddNode(new PieceNode(piece));
			}*/
		}
		
		// Pieces
		private void DoCoefficientsEdited(object source, EditedArgs args)
		{
			/*PieceNode node = (PieceNode)d_pieceStore.GetNode(new TreePath(args.Path));
			node.Coefficients = args.NewText;*/
		}
		
		private void DoBeginEdited(object source, EditedArgs args)
		{
			//PieceNode node = (PieceNode)d_pieceStore.GetNode(new TreePath(args.Path));
			//node.Begin = args.NewText;
		}
		
		private void DoEndEdited(object source, EditedArgs args)
		{
			//PieceNode node = (PieceNode)d_pieceStore.GetNode(new TreePath(args.Path));
			//node.End = args.NewText;
		}
		
		private void DoPieceSelectionChanged(object source, EventArgs args)
		{
			//NodeSelection selection = source as NodeSelection;

			//d_pieceRemoveButton.Sensitive = selection.SelectedNodes.Length != 0;
		}
		
		private void DoPieceTreeViewKeyPress(object sender, KeyPressEventArgs args)
		{
			//if (args.Event.Key == Gdk.Key.Delete)
			//{
			//	DoRemovePiece(sender, new EventArgs());
			//}
		}
		
		private void DoRemovePiece(object sender, EventArgs args)
		{
			/*NodeSelection selection = d_pieceTreeview.NodeSelection;
			PolynomialNode function = (PolynomialNode)d_treeview.NodeSelection.SelectedNodes[0];
			
			PieceNode[] nodes = new PieceNode[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (PieceNode node in nodes)
			{
				function.Function.Remove(node.Polynomial);
				d_pieceStore.RemoveNode(node);
			}*/
		}
		
		private void DoAddPiece(object sender, EventArgs args)
		{
			/*PolynomialNode node = (PolynomialNode)d_treeview.NodeSelection.SelectedNodes[0];
			
			FunctionPolynomialPiece[] pieces = node.Function.Pieces;
			double begin = 0;
			double end = 1;
			
			if (pieces.Length != 0)
			{
				begin = pieces[pieces.Length - 1].End;
				end = begin + 1;
			}
			
			FunctionPolynomialPiece piece = new FunctionPolynomialPiece(begin, end, 0, 1);
			node.Function.Add(piece);
			
			ITreeNode newnode = new PieceNode(piece);
			d_pieceStore.AddNode(newnode);
			
			d_pieceTreeview.NodeSelection.SelectNode(newnode);*/
		}
		
		private void DoInterpolate(object sender, EventArgs args)
		{
			/*PolynomialNode function = (PolynomialNode)d_treeview.NodeSelection.SelectedNodes[0];
			Dialogs.Interpolate dlg = new Dialogs.Interpolate(Toplevel as Gtk.Window, function.Function);
			
			dlg.Show();

			dlg.Response += delegate(object o, ResponseArgs a1) {
				if (a1.ResponseId == ResponseType.Apply)
				{
					//ApplyInterpolation(function, dlg.Interpolation);
				}
				
				dlg.Destroy();
			};*/
		}

		/*private void ApplyInterpolation(PolynomialNode node, Interpolators.Interpolation interpolation)
		{
			if (interpolation == null)
			{
				return;
			}
			
			node.Function.Clear();

			foreach (Interpolators.Interpolation.Piece piece in interpolation.Pieces)
			{
				node.Function.Add(new Cpg.FunctionPolynomialPiece(piece.Begin, piece.End, piece.Coefficients));
			}
			
			if (d_treeview.NodeSelection.NodeIsSelected(node) && d_treeview.NodeSelection.SelectedNodes.Length == 1)
			{
				FillPieces();
			}
		}*/
	}
}
