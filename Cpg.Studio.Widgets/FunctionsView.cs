using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets
{
	public class FunctionsView : FunctionsHelper<FunctionNode, Wrappers.Function>
	{
		private Button d_removeButton;

		public FunctionsView(Actions actions, Wrappers.Network network) : base(actions, network)
		{
			InitUi();
		}
		
		private void InitUi()
		{
			// Name column
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoNameEdited;

			TreeViewColumn column = new TreeViewColumn("Name", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 75;
			TreeView.AppendColumn(column);
			
			// Arguments column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoArgumentsEdited;

			column = new TreeViewColumn("Arguments", renderer, "text", 1);
			column.Resizable = true;
			column.MinWidth = 100;
			TreeView.AppendColumn(column);
			
			// Expression column
			renderer = new CellRendererText();
			renderer.Editable = true;
			renderer.Edited += DoExpressionEdited;

			column = new TreeViewColumn("Expression", renderer, "text", 2);
			column.Resizable = true;
			column.MinWidth = 300;

			TreeView.AppendColumn(column);
			
			ScrolledWindow vw = new ScrolledWindow();

			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			vw.Add(TreeView);

			TreeView.Show();
			vw.Show();
			
			PackStart(vw, true, true, 0);
			
			HBox hbox = new HBox(false, 3);
			hbox.Show();
			
			Alignment align = new Alignment(0, 0, 1, 1);
			align.Show();

			align.SetPadding(0, 0, 6, 0);
			align.Add(hbox);
			
			PackStart(align, false, false, 0);

			d_removeButton = new Button();
			d_removeButton.Add(new Image(Gtk.Stock.Remove, IconSize.Menu));
			d_removeButton.Sensitive = false;
			d_removeButton.Clicked += delegate {
				RemoveSelection();
			};
			d_removeButton.ShowAll();

			hbox.PackStart(d_removeButton, false, false, 0);

			Button but = new Button();
			but.Add(new Image(Gtk.Stock.Add, IconSize.Menu));
			but.Clicked += DoAdd;
			but.ShowAll();

			hbox.PackStart(but, false, false, 0);

			TreeView.Selection.Changed += DoSelectionChanged;
		}
		
		private void DoAdd(object sender, EventArgs args)
		{
			int i = 1;
			string funcName;

			while (true)
			{
				funcName = String.Format("f{0}", i++);

				if (Group.GetFunction(funcName) == null)
				{
					break;
				}
			}

			Wrappers.Function function = new Wrappers.Function(funcName, "x");
			function.AddArgument(new FunctionArgument("x", false, 0));

			Add(function);
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			Wrappers.Function f = FromStorage(args.Path).Function;

			if (args.NewText == String.Empty || f.Id == args.NewText.Trim())
			{
				return;
			}

			Actions.Do(new Undo.ModifyObjectId(f, args.NewText));
		}
		
		private Cpg.FunctionArgument[] ParseArguments(string args)
		{
			List<Cpg.FunctionArgument> ret = new List<Cpg.FunctionArgument>();
			bool optional = false;
			
			if (args.Trim() == String.Empty)
			{
				return new Cpg.FunctionArgument[] {};
			}
			
			foreach (string arg in args.Split(','))
			{
				string[] parts = arg.Split(new char[] {'='}, 2);
				double optval = 0;
				
				if (parts.Length == 2)
				{
					optional = true;
					optval = Double.Parse(parts[1].Trim());
				}
				
				ret.Add(new Cpg.FunctionArgument(parts[0].Trim(), optional, optval));
			}

			return ret.ToArray();
		}
		
		private bool CompareArgument(Cpg.FunctionArgument a1, Cpg.FunctionArgument a2)
		{
			if (a1.Name != a2.Name)
			{
				return false;
			}
			
			if (a1.Optional != a2.Optional)
			{
				return false;
			}
			
			if (a1.DefaultValue != a2.DefaultValue)
			{
				return false;
			}
			
			return true;
		}
		
		private bool CompareArguments(Cpg.FunctionArgument[] a1, Cpg.FunctionArgument[] a2)
		{
			if (a1.Length != a2.Length)
			{
				return false;
			}
			
			for (int i = 0; i < a1.Length; ++i)
			{
				if (!CompareArgument(a1[i], a2[i]))
				{
					return false;
				}
			}
			
			return true;
		}
		
		private void DoArgumentsEdited(object source, EditedArgs args)
		{
			Wrappers.Function f = FromStorage(args.Path).Function;

			Cpg.FunctionArgument[] arguments = ParseArguments(args.NewText);
			
			if (!CompareArguments(arguments, f.Arguments))
			{
				Actions.Do(new Undo.ModifyFunctionArguments(f, arguments));
			}
		}
		
		private void DoExpressionEdited(object source, EditedArgs args)
		{
			Wrappers.Function f = FromStorage(args.Path).Function;

			if (args.NewText.Trim() == f.Expression.AsString.Trim())
			{
				return;
			}

			Actions.Do(new Undo.ModifyExpression(f.Expression, args.NewText.Trim()));
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			d_removeButton.Sensitive = TreeView.Selection.CountSelectedRows() != 0;
		}
	}
}
