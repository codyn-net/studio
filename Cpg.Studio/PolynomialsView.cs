using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio
{
	public class PolynomialsView : VBox
	{
		private Wrappers.Network d_network;
		private NodeStore d_store;
		private NodeView d_treeview;
		
		private NodeStore d_pieceStore;
		private NodeView d_pieceTreeview;
		
		private Gtk.Button d_removeButton;
		private Gtk.Button d_pieceRemoveButton;
		
		private Gtk.HPaned d_paned;
		
		class PolynomialNode : TreeNode
		{
			Cpg.FunctionPolynomial d_polynomial;
			
			public PolynomialNode(Cpg.FunctionPolynomial polynomial)
			{
				d_polynomial = polynomial;
			}
			
			[TreeNodeValue(Column=0)]
			public string Name
			{
				get
				{
					return d_polynomial.Id;
				}
				set
				{
					d_polynomial.Id = value;
				}
			}
			
			public FunctionPolynomial Function
			{
				get
				{
					return d_polynomial;
				}
			}
		}
		
		class PieceNode : TreeNode
		{
			Cpg.FunctionPolynomialPiece d_piece;
			
			public PieceNode(Cpg.FunctionPolynomialPiece piece)
			{
				d_piece = piece;
			}
			
			[TreeNodeValue(Column=0)]
			public string Begin
			{
				get
				{
					return String.Format("{0}", d_piece.Begin);
				}
				set
				{
					d_piece.Begin = Double.Parse(value);
				}
			}
			
			[TreeNodeValue(Column=1)]
			public string End
			{
				get
				{
					return String.Format("{0}", d_piece.End);
				}
				set
				{
					d_piece.End = Double.Parse(value);
				}
			}
			
			[TreeNodeValue(Column=2)]
			public string Coefficients
			{
				get
				{
					double[] coefficients = d_piece.Coefficients;
					string[] ret = new string[coefficients.Length];
					
					for (int i = 0; i < coefficients.Length; ++i)
					{
						ret[i] = String.Format("{0}", coefficients[i]);
					}
					
					return String.Join(", ", ret);
				}
				set
				{
					List<double> coefficients = new List<double>();
					foreach (string coef in value.Split(',', ' '))
					{
						if (!String.IsNullOrEmpty(coef))
						{
							coefficients.Add(Double.Parse(coef));
						}
					}
					
					d_piece.Coefficients = coefficients.ToArray();
				}
			}
			
			public Cpg.FunctionPolynomialPiece Polynomial
			{
				get
				{
					return d_piece;
				}
			}
		}
		
		class PolynomialNodeStore : Gtk.NodeStore
		{
			public PolynomialNodeStore() : base(typeof(PolynomialNode))
			{
			}
		}
		
		class PieceNodeStore : Gtk.NodeStore
		{
			public PieceNodeStore() : base(typeof(PieceNode))
			{
			}
		}

		public PolynomialsView(Wrappers.Network network) : base(false, 3)
		{
			d_network = network;

			InitUi();
		}
		
		private void InitUi()
		{
			d_store = new PolynomialNodeStore();
			d_treeview = new NodeView(d_store);
			d_treeview.ShowExpanders = false;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			
			d_paned = new Gtk.HPaned();
			d_paned.Show();
			
			PackStart(d_paned, true, true, 0);
			
			// Name column
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoNameEdited;

			TreeViewColumn column = new TreeViewColumn("Name", renderer, new object[] {"text", 0});
			column.Resizable = true;
			column.MinWidth = 75;

			d_treeview.AppendColumn(column);			
			ScrolledWindow vw = new ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(d_treeview);

			d_treeview.Show();
			vw.Show();

			d_paned.Add1(vw);
			
			HBox hbox = new HBox(false, 3);
			hbox.Show();
			
			PackStart(hbox, false, false, 0);

			d_treeview.KeyPressEvent += DoTreeViewKeyPress;
			
			d_removeButton = new Button();
			d_removeButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeButton.Sensitive = false;
			d_removeButton.Clicked += DoRemove;
			d_removeButton.ShowAll();

			hbox.PackStart(d_removeButton, false, false ,0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAdd;
			but.ShowAll();

			hbox.PackStart(but, false, false, 0);

			d_treeview.NodeSelection.Changed += DoSelectionChanged;
				
			InitStore();
			
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
			
			vw = new ScrolledWindow();

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
			
			hbox.PackEnd(but, false, false, 0);

			d_pieceTreeview.NodeSelection.Changed += DoPieceSelectionChanged;
			d_paned.Add2(vbox);
			
			d_paned.Position = 150;
			FillPieces();
		}
		
		private void InitStore()
		{
			foreach (Cpg.Function function in d_network.Functions)
			{
				if (function is Cpg.FunctionPolynomial)
				{
					d_store.AddNode(new PolynomialNode((Cpg.FunctionPolynomial)function));
				}
			}
		}
		
		private void DoRemove(object sender, EventArgs args)
		{
			NodeSelection selection = d_treeview.NodeSelection;
			
			PolynomialNode[] nodes = new PolynomialNode[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (PolynomialNode node in nodes)
			{
				d_network.FunctionGroup.Remove(node.Function);
				d_store.RemoveNode(node);
			}
		}
		
		private void DoAdd(object sender, EventArgs args)
		{
			int i = 1;
			string funcName;

			while (true)
			{
				funcName = String.Format("f{0}", i++);

				if (d_network.GetFunction(funcName) == null)
				{
					break;
				}
			}

			Cpg.FunctionPolynomial function = new Cpg.FunctionPolynomial(funcName);
			d_network.FunctionGroup.Add(function);
			
			ITreeNode newnode = new PolynomialNode(function);
			d_store.AddNode(newnode);
			
			d_treeview.NodeSelection.SelectNode(newnode);
		}
		
		private void DoTreeViewKeyPress(object sender, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				DoRemove(sender, new EventArgs());
			}
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			if (args.NewText == String.Empty)
			{
				return;
			}

			PolynomialNode node = (PolynomialNode)d_store.GetNode(new TreePath(args.Path));
			node.Name = args.NewText;
		}

		private void DoSelectionChanged(object source, EventArgs args)
		{
			NodeSelection selection = source as NodeSelection;
			
			d_removeButton.Sensitive = selection.SelectedNodes.Length != 0;
			FillPieces();
		}
		
		private void FillPieces()
		{
			NodeSelection selection = d_treeview.NodeSelection;
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
			}
		}
		
		// Pieces
		private void DoCoefficientsEdited(object source, EditedArgs args)
		{
			PieceNode node = (PieceNode)d_pieceStore.GetNode(new TreePath(args.Path));
			node.Coefficients = args.NewText;
		}
		
		private void DoBeginEdited(object source, EditedArgs args)
		{
			PieceNode node = (PieceNode)d_pieceStore.GetNode(new TreePath(args.Path));
			node.Begin = args.NewText;
		}
		
		private void DoEndEdited(object source, EditedArgs args)
		{
			PieceNode node = (PieceNode)d_pieceStore.GetNode(new TreePath(args.Path));
			node.End = args.NewText;
		}
		
		private void DoPieceSelectionChanged(object source, EventArgs args)
		{
			NodeSelection selection = source as NodeSelection;

			d_pieceRemoveButton.Sensitive = selection.SelectedNodes.Length != 0;
		}
		
		private void DoPieceTreeViewKeyPress(object sender, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				DoRemovePiece(sender, new EventArgs());
			}
		}
		
		private void DoRemovePiece(object sender, EventArgs args)
		{
			NodeSelection selection = d_pieceTreeview.NodeSelection;
			PolynomialNode function = (PolynomialNode)d_treeview.NodeSelection.SelectedNodes[0];
			
			PieceNode[] nodes = new PieceNode[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (PieceNode node in nodes)
			{
				function.Function.Remove(node.Polynomial);
				d_pieceStore.RemoveNode(node);
			}
		}
		
		private void DoAddPiece(object sender, EventArgs args)
		{
			PolynomialNode node = (PolynomialNode)d_treeview.NodeSelection.SelectedNodes[0];
			
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
			
			d_pieceTreeview.NodeSelection.SelectNode(newnode);
		}
		
		private void DoInterpolate(object sender, EventArgs args)
		{
			PolynomialNode function = (PolynomialNode)d_treeview.NodeSelection.SelectedNodes[0];
			Dialogs.Interpolate dlg = new Dialogs.Interpolate(Toplevel as Gtk.Window, function.Function);
			
			dlg.Show();

			dlg.Response += delegate(object o, ResponseArgs a1) {
				if (a1.ResponseId == ResponseType.Apply)
				{
					ApplyInterpolation(function, dlg.Interpolation);
				}
				
				dlg.Destroy();
			};
		}

		private void ApplyInterpolation(PolynomialNode node, Interpolators.Interpolation interpolation)
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
		}
	}
}
