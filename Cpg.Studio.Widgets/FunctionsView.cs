using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class FunctionsView : VBox
	{
		private Cpg.Studio.Wrappers.Network d_network;
		private NodeStore d_store;
		private NodeView d_treeview;
		private Gtk.Button d_removeButton;
		
		class Node : TreeNode
		{
			Cpg.Function d_function;
			
			public Node(Cpg.Function function)
			{
				d_function = function;
			}
			
			[TreeNodeValue(Column=0)]
			public string Name
			{
				get
				{
					return d_function.Id;
				}
				set
				{
					d_function.Id = value;
				}
			}
			
			[TreeNodeValue(Column=1)]
			public string Arguments
			{
				get
				{
					Cpg.FunctionArgument[] arguments = d_function.Arguments;
					string[] ret = new string[arguments.Length];
					
					bool optional = false;
					
					for (int i = 0; i < arguments.Length; ++i)
					{
						double optval = 0;

						if (arguments[i].Optional)
						{
							optional = true;
							optval = arguments[i].DefaultValue;
						}

						ret[i] = arguments[i].Name;
						
						if (optional)
						{
							ret[i] += String.Format(" = {0}", optval);
						}
					}
					
					return String.Join(", ", ret);
				}
				set
				{
					List<string> parts = new List<string>(value.Split(','));
					parts.RemoveAll(delegate (string r) {
						return String.IsNullOrEmpty(r);
					});
					
					d_function.ClearArguments();

					bool optional = false;
					
					foreach (string part in parts)
					{
						string[] opt = part.Trim().Split(new char[] {'='}, 2);
						double optval = 0;
						
						if (opt.Length == 2)
						{
							optional = true;
							optval = Double.Parse(opt[1].Trim());
						}
						
						d_function.AddArgument(new FunctionArgument(opt[0].Trim(), optional, optval));
					}
				}
			}
			
			[TreeNodeValue(Column=2)]
			public string Expression
			{
				get
				{
					return d_function.Expression.AsString;
				}
				set
				{
					d_function.Expression = new Expression(value);
				}
			}
			
			public Cpg.Function Function
			{
				get
				{
					return d_function;
				}
			}
		}
		
		class NodeStore : Gtk.NodeStore
		{
			public NodeStore() : base(typeof(Node))
			{
			}
		}		

		public FunctionsView(Cpg.Studio.Wrappers.Network network) : base(false, 3)
		{
			d_network = network;
			
			InitUi();
		}
		
		private void InitUi()
		{
			d_store = new NodeStore();
			d_treeview = new NodeView(d_store);
			d_treeview.ShowExpanders = false;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			
			// Name column
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoNameEdited;

			TreeViewColumn column = new TreeViewColumn("Name", renderer, new object[] {"text", 0});
			column.Resizable = true;
			column.MinWidth = 75;

			d_treeview.AppendColumn(column);
			
			// Arguments column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoArgumentsEdited;

			column = new TreeViewColumn("Arguments", renderer, new object[] {"text", 1});
			column.Resizable = true;
			column.MinWidth = 100;

			d_treeview.AppendColumn(column);
			
			// Expression column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoExpressionEdited;

			column = new TreeViewColumn("Expression", renderer, new object[] {"text", 2});
			column.Resizable = true;
			column.MinWidth = 300;

			d_treeview.AppendColumn(column);
			
			ScrolledWindow vw = new ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(d_treeview);

			d_treeview.Show();
			vw.Show();
			
			PackStart(vw, true, true, 0);
			
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
		}
		
		private void InitStore()
		{
			foreach (Cpg.Function function in d_network.Functions)
			{
				if (!(function is Cpg.FunctionPolynomial))
				{
					d_store.AddNode(new Node(function));
				}
			}
		}
		
		private void DoRemove(object sender, EventArgs args)
		{
			NodeSelection selection = d_treeview.NodeSelection;
			
			Node[] nodes = new Node[selection.SelectedNodes.Length];
			selection.SelectedNodes.CopyTo(nodes, 0);
			
			foreach (Node node in nodes)
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

			Cpg.Function function = new Cpg.Function(funcName, "x");
			function.AddArgument(new FunctionArgument("x", false, 0));

			d_network.FunctionGroup.Add(function);
			
			d_store.AddNode(new Node(function));
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

			Node node = (Node)d_store.GetNode(new TreePath(args.Path));
			node.Name = args.NewText;
		}
		
		private void DoArgumentsEdited(object source, EditedArgs args)
		{
			Node node = (Node)d_store.GetNode(new TreePath(args.Path));
			node.Arguments = args.NewText;
		}
		
		private void DoExpressionEdited(object source, EditedArgs args)
		{
			if (args.NewText == String.Empty)
			{
				return;
			}

			Node node = (Node)d_store.GetNode(new TreePath(args.Path));
			node.Expression = args.NewText;
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			NodeSelection selection = source as NodeSelection;
			d_removeButton.Sensitive = selection.SelectedNodes.Length != 0;
		}
	}
}
