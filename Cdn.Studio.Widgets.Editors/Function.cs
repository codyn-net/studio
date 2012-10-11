using System;
using Gtk;
using System.Collections.Generic;

namespace Cdn.Studio.Widgets.Editors
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.KP_Subtract, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding"),
	 Gtk.Binding(Gdk.Key.KP_Add, "HandleAddBinding")]
	public class Function : VBox
	{
		private class Node : Widgets.Node
		{
			public enum Column
			{
				Name,
				Default,
				Implicit,
				Editable
			}
			
			private Cdn.FunctionArgument d_argument;
			
			public Node() : this(null)
			{
			}

			public Node(Cdn.FunctionArgument argument)
			{
				d_argument = argument;
				
				if (d_argument != null)
				{				
					d_argument.AddNotification("name", OnChanged);
					d_argument.AddNotification("default-value", OnChanged);
					d_argument.AddNotification("explicit", OnChanged);
				}
			}
			
			public override void Dispose()
			{
				base.Dispose();
				
				if (d_argument != null)
				{			
					d_argument.RemoveNotification("name", OnChanged);
					d_argument.RemoveNotification("default-value", OnChanged);
					d_argument.RemoveNotification("explicit", OnChanged);
				}
			}
			
			private void OnChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[NodeColumn(Column.Name), PrimaryKey]
			public string Name
			{
				get { return d_argument != null ? d_argument.Name : "Add..."; }
			}
			
			[NodeColumn(Column.Default)]
			public string Default
			{
				get { return (d_argument != null && d_argument.Optional) ? d_argument.DefaultValue.AsString : null; }
			}
			
			[NodeColumn(Column.Implicit)]
			public bool Implicit
			{
				get { return d_argument == null ? false : !d_argument.Explicit; }
			}
			
			[PrimaryKey]
			public Cdn.FunctionArgument Argument
			{
				get { return d_argument; }
			}
			
			[NodeColumn(Column.Editable)]
			public bool Editable
			{
				get
				{
					return d_argument != null;
				}
			}
		}

		private Wrappers.Function d_function;
		private Entry d_expression;
		private Actions d_actions;
		private TreeView<Node> d_treeview;
		private Node d_dummy;
		private Entry d_editingEntry;
		private string d_editingPath;
		private bool d_selectArgument;
		
		public delegate void ErrorHandler(object source, Exception exception);
		public event ErrorHandler Error = delegate {};

		public Function(Wrappers.Function function, Actions actions) : base(false, 6)
		{
			d_actions = actions;
			d_function = function;

			Build();
		}
		
		private void Build()
		{
			d_expression = new Entry();
			d_expression.Show();
			
			if (d_function != null)
			{
				d_expression.Text = d_function.Expression.AsString;
				
				d_expression.FocusOutEvent += delegate {
					SaveExpression();
				};
				
				d_expression.Activated += delegate {
					SaveExpression();
				};
			}
			
			d_expression.TooltipText = "Function expression";

			d_treeview = new TreeView<Node>();
			d_treeview.Show();
			d_treeview.EnableSearch = false;
			
			d_treeview.RulesHint = true;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			d_treeview.ShowExpanders = false;

			d_treeview.ButtonPressEvent += OnTreeViewButtonPressEvent;
			d_treeview.KeyPressEvent += OnTreeViewKeyPressEvent;
			
			d_treeview.QueryTooltip += OnTreeViewQueryTooltip;
			d_treeview.HasTooltip = true;
			
			CellRenderer renderer;
			TreeViewColumn column;
			
			renderer = new CellRendererText();
			column = new TreeViewColumn("Argument", renderer, "text", Node.Column.Name);
			d_treeview.AppendColumn(column);
			column.MinWidth = 100;
			
			column.SetCellDataFunc(renderer, delegate (TreeViewColumn col, CellRenderer rend, TreeModel model, TreeIter iter)  {
				Node node = d_treeview.NodeStore.GetFromIter(iter);
				CellRendererText text = rend as CellRendererText;
				
				if (node.Argument == null)
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
				
				if (node.Argument == null)
				{
					d_editingEntry.Text = "";
				}
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
				{
					OnEntryKeyPressed(a, rname, NameEdited);
				};
			};

			renderer.EditingCanceled += delegate(object sender, EventArgs e) {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					NameEdited(d_editingEntry.Text, d_editingPath);
				}
			};
			
			rname.Edited += DoNameEdited;
			rname.Editable = true;
			
			renderer = new CellRendererText();
			column = new TreeViewColumn("Default Value", renderer, "text", Node.Column.Default, "editable", Node.Column.Editable);
			d_treeview.AppendColumn(column);
			column.MinWidth = 100;
			
			CellRendererText rdef = renderer as CellRendererText;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				d_editingEntry = args.Editable as Entry;
				d_editingPath = args.Path;
				
				Node node = d_treeview.NodeStore.FindPath(new TreePath(args.Path));
				
				if (node.Argument == null)
				{
					d_editingEntry.Text = "";
				}
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
				{
					OnEntryKeyPressed(a, rdef, DefaultValueEdited);
				};
			};

			renderer.EditingCanceled += delegate(object sender, EventArgs e) {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					DefaultValueEdited(d_editingEntry.Text, d_editingPath);
				}
			};
			
			rdef.Edited += DoDefaultValueEdited;
			
			renderer = new CellRendererToggle();
			column = new TreeViewColumn("Implicit", renderer, "active", Node.Column.Implicit, "activatable", Node.Column.Editable);
			d_treeview.AppendColumn(column);
			
			CellRendererToggle toggle = renderer as CellRendererToggle;
			
			toggle.Toggled += DoImplicitToggled;
			
			d_treeview.AppendColumn(new TreeViewColumn());
			
			ScrolledWindow sw = new ScrolledWindow();
			sw.Add(d_treeview);
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			sw.ShadowType = ShadowType.EtchedIn;
			sw.Show();

			PackStart(sw, true, true, 0);
			
			HBox hbox = new HBox(false, 6);
			hbox.Show();

			Label lbl = new Label("Expression:");
			lbl.Show();
			
			hbox.PackStart(lbl, false, false, 0);
			hbox.PackStart(d_expression, true, true, 0);

			PackStart(hbox, false, true, 0);
			
			Populate();
		}

		private void OnTreeViewQueryTooltip(object o, QueryTooltipArgs args)
		{
			int x;
			int y;

			d_treeview.ConvertWidgetToBinWindowCoords(args.X, args.Y, out x, out y);
			
			TreePath path;
			TreeViewColumn column;
			args.RetVal = false;
			
			if (y < 0)
			{
				y = 0;
			}

			if (d_treeview.GetPathAtPos(x, y, out path, out column))
			{
				int idx = Array.IndexOf(d_treeview.Columns, column);

				switch (idx)
				{
					case 0:
						args.Tooltip.Text = "Name of the argument";
						args.RetVal = true;
					break;
					case 1:
						args.Tooltip.Text = "The default value of the argument (if defined the argument is optional)";
						args.RetVal = true;
					break;
					case 2:
						args.Tooltip.Text = "Explicit arguments are function parameters, implicit arguments are looked up in the executing context (i.e. caller properties)";
						args.RetVal = true;
					break;
					default:
					break;
				}
			}
		}
		
		private void Repopulate()
		{
			d_treeview.NodeStore.Clear();
			
			if (d_function == null)
			{
				return;
			}
			
			foreach (Cdn.FunctionArgument argument in d_function.Arguments)
			{
				d_treeview.NodeStore.Add(new Node(argument));
			}
			
			d_dummy = new Node();
			d_treeview.NodeStore.Add(d_dummy);
		}
		
		private void Populate()
		{
			Repopulate();			
			Connect();
		}
		
		public override void Destroy()
		{
			Disconnect();
			
			base.Destroy();
		}
		
		private void Connect()
		{
			if (d_function == null)
			{
				return;
			}

			d_function.ArgumentAdded += DoArgumentAdded;
			d_function.ArgumentRemoved += DoArgumentRemoved;
			
			d_function.ArgumentsReordered += DoArgumentsReordered;
		}
		
		private void Disconnect()
		{
			if (d_function == null)
			{
				return;
			}
			
			d_function.ArgumentAdded -= DoArgumentAdded;
			d_function.ArgumentRemoved -= DoArgumentRemoved;
			d_function.ArgumentsReordered -= DoArgumentsReordered;
		}
		
		private void DoArgumentsReordered(Wrappers.Function function)
		{
			Repopulate();
		}

		private void DoArgumentAdded(Wrappers.Function obj, Cdn.FunctionArgument arg)
		{
			Node node = new Node(arg);

			d_treeview.NodeStore.Remove(d_dummy);
			d_treeview.NodeStore.Add(node);
			d_treeview.NodeStore.Add(d_dummy);
			
			if (d_selectArgument)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectPath(node.Path);
				
				d_treeview.SetCursor(node.Path, d_treeview.GetColumn(0), true);
			}
		}
		
		private void DoArgumentRemoved(Wrappers.Function obj, Cdn.FunctionArgument arg)
		{
			d_treeview.NodeStore.Remove(arg);
		}
		
		private void SaveExpression()
		{
			string expr = d_expression.Text.Trim();

			if (d_expression.Text == "")
			{
				d_expression.Text = "0";
				expr = "0";
			}

			try
			{
				d_actions.Do(new Undo.ModifyExpression(d_function.Expression, expr));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
			}
		}
		
		private void DefaultValueEdited(string newVal, string path)
		{
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null || node.Argument == null)
			{
				return;
			}
			
			string val = newVal.Trim();
			
			if (val == "")
			{
				val = null;
			}
			
			if (val == node.Default)
			{
				return;
			}
			
			try
			{
				d_actions.Do(new Undo.ModifyFunctionArgumentDefaultValue(d_function, node.Argument, val));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
		}
		
		private void NameEdited(string newName, string path)
		{
			if (String.IsNullOrEmpty(newName))
			{
				return;
			}
			
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			if (node.Argument == null)
			{
				/* Add a new argument */
				try
				{
					d_actions.Do(new Undo.AddFunctionArgument(d_function, newName.Trim(), null, false));
				}
				catch (GLib.GException err)
				{
					// Display could not remove, or something
					Error(this, err);
				}
				
				return;
			}
			
			if (newName.Trim() == node.Argument.Name)
			{
				return;
			}

			try
			{
				d_actions.Do(new Undo.ModifyFunctionArgumentName(d_function, node.Argument, newName.Trim()));
			}
			catch (GLib.GException err)
			{
				// Display could not remove, or something
				Error(this, err);
				return;
			}
		}
		
		private void DoNameEdited(object source, EditedArgs args)
		{
			NameEdited(args.NewText, args.Path);
		}
		
		private void DoDefaultValueEdited(object source, EditedArgs args)
		{
			DefaultValueEdited(args.NewText, args.Path);
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

		private void DoRemoveArgument()
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				Node node = d_treeview.NodeStore.FindPath(path);
				
				if (node == d_dummy)
				{
					continue;
				}
				
				actions.Add(new Undo.RemoveFunctionArgument(d_function, node.Argument));
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
		
		private bool ArgumentExists(string name)
		{
			foreach (Cdn.FunctionArgument argument in d_function.Arguments)
			{
				if (argument.Name == name)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private void DoAddArgument()
		{
			int num = 1;
			
			while (ArgumentExists("x" + num))
			{
				++num;
			}
			
			d_selectArgument = true;
			d_actions.Do(new Undo.AddFunctionArgument(d_function, "x" + num, null, false));
			d_selectArgument = false;
		}
		
		private void ShowPopup(Gdk.EventButton evnt)
		{
			Gtk.AccelGroup grp = new Gtk.AccelGroup();
			
			Gtk.Menu menu = new Gtk.Menu();
			menu.Show();
			menu.AccelGroup = grp;
			
			MenuItem item;
			
			item = new MenuItem("Add");
			item.AccelPath = "<CdnStudio>/Widgets/Editors/Functions/Add";
			
			AccelMap.AddEntry("<CdnStudio>/Widgets/Editors/Functions/Add", (uint)Gdk.Key.KP_Add, Gdk.ModifierType.None);

			item.Show();
			item.Activated += DoAddArgument;
			
			menu.Append(item);

			item = new MenuItem("Remove");
			item.AccelPath = "<CdnStudio>/Widgets/Editors/Functions/Remove";
			item.Show();
			
			AccelMap.AddEntry("<CdnStudio>/Widgets/Editors/Functions/Remove", (uint)Gdk.Key.KP_Subtract, Gdk.ModifierType.None);
			
			item.Sensitive = (d_treeview.Selection.CountSelectedRows() > 0);
			item.Activated += DoRemoveArgument;
			
			menu.Append(item);
				
			menu.Popup(null, null, null, evnt.Button, evnt.Time);
		}
		
		private void HandleAddBinding()
		{
			DoAddArgument();
		}
		
		private void HandleDeleteBinding()
		{
			DoRemoveArgument();
		}
		
		private void DoAddArgument(object source, EventArgs args)
		{
			DoAddArgument();
		}
		
		private void DoRemoveArgument(object source, EventArgs args)
		{
			DoRemoveArgument();
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
		
		private void DoImplicitToggled(object source, ToggledArgs args)
		{
			Node node = d_treeview.NodeStore.FindPath(args.Path);
			CellRendererToggle toggle = (CellRendererToggle)source;
			
			d_actions.Do(new Undo.ModifyFunctionArgumentExplicit(d_function, node.Argument, !toggle.Active));
		}
	}
}

