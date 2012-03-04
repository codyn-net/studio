using System;
using Gtk;
using System.Collections.Generic;

namespace Cdn.Studio.Widgets.Editors
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.KP_Subtract, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding"),
	 Gtk.Binding(Gdk.Key.KP_Add, "HandleAddBinding")]
	public class Link : ScrolledWindow
	{
		private class Node : Widgets.Node
		{
			public enum Column
			{
				Target,
				Equation,
				Tooltip,
				EquationEditable
			}

			private LinkAction d_action;

			public Node(LinkAction action)
			{
				d_action = action;
				
				if (d_action != null)
				{				
					d_action.AddNotification("target", OnActionChanged);
					d_action.AddNotification("equation", OnActionChanged);
				}
			}
			
			public override void Dispose()
			{
				if (d_action != null)
				{
					d_action.RemoveNotification("target", OnActionChanged);
					d_action.RemoveNotification("equation", OnActionChanged);
				}
				
				base.Dispose();
			}
			
			private void OnActionChanged(object source,GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[PrimaryKey]
			public LinkAction LinkAction
			{
				get	{ return d_action; }
			}
			
			[NodeColumn(Column.Target)]
			public string Target
			{
				get { return d_action != null ? d_action.Target : "Add..."; }
			}
			
			[NodeColumn(Column.Equation)]
			public string Equation
			{
				get { return d_action != null ? d_action.Equation.AsString : ""; }
			}
			
			[NodeColumn(Column.Tooltip)]
			public string Tooltip
			{
				get
				{
					if (d_action == null)
					{
						return null;
					}

					List<string > parts = new List<string>();

					string annotation = d_action.Annotation;
						
					if (annotation != null)
					{
						parts.Add(annotation.Replace("\n", " "));
					}

					if (d_action.Equation != null &&
						d_action.Equation.Instructions.Length != 0)
					{
						parts.Add(String.Format("<i>Value: <tt>{0}</tt></i>", d_action.Equation.Evaluate()));

						ExpressionTreeIter it = new ExpressionTreeIter(d_action.Equation);
						it.Simplify();

						string its = it.ToStringDbg();

						if (its.Length >= 80)
						{
							its = its.Substring(0, 76) + "...";
						}

						parts.Add(String.Format("<i>Expression: <tt>{0}</tt></i>", its));
					}

					if (parts.Count == 0)
					{
						return null;
					}
						
					return String.Join("\n", parts.ToArray());
				}
			}
			
			[NodeColumn(Column.EquationEditable)]
			public bool EquationEditable
			{
				get { return d_action != null; }
			}
		}

		private Wrappers.Link d_link;
		private Actions d_actions;
		private Widgets.TreeView<Node> d_treeview;
		private bool d_selectAction;
		private Node d_dummy;
		private Entry d_editingEntry;
		private string d_editingPath;
		private CellRenderer d_rendererTarget;
		private CellRenderer d_rendererEquation;

		public Link(Wrappers.Link link, Actions actions)
		{
			d_link = link;
			d_actions = actions;
			
			Build();

			Sensitive = (d_link != null);
			Connect();
		}
		
		private void Build()
		{
			SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			ShadowType = ShadowType.EtchedIn;
			
			d_treeview = new Widgets.TreeView<Node>();
			d_treeview.Show();
			
			d_treeview.ShowExpanders = false;
			d_treeview.RulesHint = true;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			d_treeview.ButtonPressEvent += OnTreeViewButtonPressEvent;
			d_treeview.EnableSearch = false;
			d_treeview.TooltipColumn = (int)Node.Column.Tooltip;
			d_treeview.KeyPressEvent += OnTreeViewKeyPressEvent;
			
			Add(d_treeview);
			
			CellRendererText renderer;
			Gtk.TreeViewColumn column;
			
			// Target renderer
			renderer = new CellRendererText();
			column = d_treeview.AppendColumn("Target", renderer, "text", Node.Column.Target);
			
			column.SetCellDataFunc(renderer, VisualizeProperties);

			d_rendererTarget = renderer;

			renderer.Editable = true;
			renderer.Edited += HandleLinkActionTargetEdited;
			renderer.EditingStarted += HandleLinkActionTargetEditingStarted;
			renderer.EditingCanceled += delegate {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					TargetEdited(d_editingEntry.Text, d_editingPath);
				}
			};

			column.MinWidth = 80;
			
			// Equation renderer
			renderer = new CellRendererText();
			renderer.Editable = true;
			d_rendererEquation = renderer;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs a) {
				d_editingEntry = a.Editable as Entry;
				d_editingPath = a.Path;
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs args) {
					OnEntryKeyPressed(args, d_rendererEquation, EquationEdited);
				};
			};

			renderer.Edited += HandleLinkActionEquationEdited;
			
			renderer.EditingCanceled += delegate {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					EquationEdited(d_editingEntry.Text, d_editingPath);
				}
			};
			
			column = d_treeview.AppendColumn("Equation", renderer, "text", Node.Column.Equation, "editable", Node.Column.EquationEditable);
			column.Expand = true;
			
			column.SetCellDataFunc(renderer, VisualizeProperties);
			
			Populate();
		}

		private void HandleLinkActionTargetEditingStarted(object o, EditingStartedArgs args)
		{
			Entry entry = args.Editable as Entry;
			
			if (entry == null)
			{
				return;
			}
			
			d_editingEntry = entry;
			d_editingPath = args.Path;
			
			if (d_treeview.NodeStore.FindPath(args.Path).LinkAction == null)
			{
				entry.Text = "";
			}
			
			d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a) {
				OnEntryKeyPressed(a, d_rendererTarget, TargetEdited);
			};
			
			if (d_link.To == null)
			{
				return;
			}
			
			EntryCompletion completion = new EntryCompletion();
			ListStore props = new ListStore(typeof(string));
			Dictionary<string, bool > found = new Dictionary<string, bool>();

			Wrappers.Group grp = d_link.To as Wrappers.Group;
			
			if (grp != null)
			{
				foreach (string name in grp.PropertyInterface.Names)
				{
					props.AppendValues(name);
					found[name] = true;
				}
			}

			foreach (Property prop in d_link.To.Properties)
			{
				if (!found.ContainsKey(prop.Name))
				{
					props.AppendValues(prop.Name);
				}
			}

			completion.Model = props;
			completion.TextColumn = 0;
			completion.InlineSelection = true;
			completion.InlineCompletion = true;

			entry.Completion = completion;
		}
		
		protected override void OnDestroyed()
		{
			Disconnect();
			base.OnDestroyed();
		}
		
		private void Connect()
		{
			d_link.ActionAdded += HandleLinkActionAdded;
			d_link.ActionRemoved += HandleLinkActionRemoved;
		}
		
		private void Disconnect()
		{
			d_link.ActionAdded -= HandleLinkActionAdded;
			d_link.ActionRemoved -= HandleLinkActionRemoved;
		}
		
		private void Populate()
		{					
			if (d_link == null)
			{
				return;
			}

			foreach (Cdn.LinkAction action in d_link.Actions)
			{
				AddLinkAction(action);
			}
			
			d_dummy = new Node(null);
			d_treeview.NodeStore.Add(d_dummy);
		}
		
		private void HandleLinkActionRemoved(object source, Cdn.LinkAction action)
		{
			d_treeview.NodeStore.Remove(action);
		}

		private void HandleLinkActionAdded(object source, Cdn.LinkAction action)
		{
			d_treeview.NodeStore.Remove(d_dummy);
			AddLinkAction(action);
			d_treeview.NodeStore.Add(d_dummy);
		}
		
		private void AddLinkAction(Cdn.LinkAction action)
		{
			TreeIter iter;
			
			d_treeview.NodeStore.Add(new Node(action), out iter);
			
			if (d_selectAction)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
			}			
		}
		
		private void TargetEdited(string text, string path)
		{
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node.LinkAction != null && node.LinkAction.Target == text.Trim())
			{
				return;
			}
			
			if (String.IsNullOrEmpty(text.Trim()))
			{
				return;
			}
			
			if (node.LinkAction == null)
			{
				d_selectAction = true;
				d_actions.Do(new Undo.AddLinkAction(d_link, text.Trim(), ""));
				d_selectAction = false;
			}
			else
			{
				d_actions.Do(new Undo.ModifyLinkActionTarget(d_link, node.LinkAction.Target, text.Trim()));
			}
		}

		private void HandleLinkActionTargetEdited(object o, EditedArgs args)
		{
			TargetEdited(args.NewText, args.Path);
		}
		
		private void HandleLinkActionEquationEdited(object o, EditedArgs args)
		{
			EquationEdited(args.NewText, args.Path);
		}
		
		private void EquationEdited(string text, string path)
		{
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node.LinkAction.Equation.AsString == text.Trim())
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyLinkActionEquation(d_link, node.LinkAction.Target, text.Trim()));
		}

		private void DoAddAction()
		{
			List<string > props = new List<string>(Array.ConvertAll<LinkAction, string>(d_link.Actions, item => item.Target));
			List<string > prefs = new List<string>();
			
			if (d_link.To != null)
			{
				prefs = new List<string>(Array.ConvertAll<Property, string>(d_link.To.Properties, item => item.Name));
			}
			else
			{
				prefs = new List<string>();
			}
			
			int i = 0;
			string name;
			
			do
			{
				if (i < prefs.Count)
				{
					name = prefs[i];
				}
				else
				{
					name = String.Format("x{0}", i - prefs.Count + 1);
				}

				++i;
			} while (props.Contains(name));
			
			d_selectAction = true;
			d_actions.Do(new Undo.AddLinkAction(d_link, name, ""));
			d_selectAction = false;

			TreePath path = d_treeview.Selection.GetSelectedRows()[0];
			d_treeview.SetCursor(path, d_treeview.GetColumn(0), true);
		}
		
		private void DoRemoveAction()
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				Node node = d_treeview.NodeStore.FindPath(path);
				
				if (node != d_dummy)
				{
					actions.Add(new Undo.RemoveLinkAction(d_link, node.LinkAction));
				}
			}
			
			d_actions.Do(new Undo.Group(actions));
		}

		private void ShowPopup(Gdk.EventButton evnt)
		{
			Gtk.AccelGroup grp = new Gtk.AccelGroup();
			
			Gtk.Menu menu = new Gtk.Menu();
			menu.Show();
			menu.AccelGroup = grp;
			
			MenuItem item;
			
			item = new MenuItem("Add");
			item.AccelPath = "<CdnStudio>/Widgets/Editors/Properties/Add";
			
			AccelMap.AddEntry("<CdnStudio>/Widgets/Editors/Properties/Add", (uint)Gdk.Key.KP_Add, Gdk.ModifierType.None);

			item.Show();
			item.Activated += delegate {
				DoAddAction(); };
			
			menu.Append(item);

			item = new MenuItem("Remove");
			item.AccelPath = "<CdnStudio>/Widgets/Editors/Properties/Remove";
			item.Show();
			
			AccelMap.AddEntry("<CdnStudio>/Widgets/Editors/Properties/Remove", (uint)Gdk.Key.KP_Subtract, Gdk.ModifierType.None);
			
			item.Sensitive = (d_treeview.Selection.CountSelectedRows() > 0);
			item.Activated += delegate {
				DoRemoveAction(); };
			
			menu.Append(item);
				
			menu.Popup(null, null, null, evnt.Button, evnt.Time);
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
						d_treeview.SetCursor(path, d_treeview.Columns[0], true);

						args.RetVal = true;
						return;
					}
				}
			}

			if (args.Event.Type != Gdk.EventType.TwoButtonPress && args.Event.Type != Gdk.EventType.ThreeButtonPress)
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
		
		private void HandleAddBinding()
		{
			DoAddAction();
		}
		
		private void HandleDeleteBinding()
		{
			DoRemoveAction();
		}
		
		private void VisualizeProperties(TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			CellRendererText text = renderer as CellRendererText;
			Node node = d_treeview.NodeStore.GetFromIter(iter);
			
			bool fromtemp = node.LinkAction != null ? (d_link.GetActionTemplate(node.LinkAction, true) != null) : false;
			bool overridden = node.LinkAction != null ? (d_link.GetActionTemplate(node.LinkAction, false) != null) : false;
			
			text.Weight = (int)Pango.Weight.Normal;
			text.Style = Pango.Style.Normal;
			
			if (node.LinkAction == null)
			{
				text.ForegroundGdk = d_treeview.Style.Foreground(StateType.Insensitive);
				text.Style = Pango.Style.Italic;
			}
			else if (d_link.To == null || d_link.To.Property(node.Target) == null)
			{
				text.Foreground = "#ff0000";
			}
			else if (fromtemp || overridden)
			{
				text.ForegroundGdk = d_treeview.Style.Foreground(Gtk.StateType.Insensitive);
				
				if (!fromtemp && overridden && col.Title == "Equation")
				{
					text.ForegroundGdk = d_treeview.Style.Foreground(d_treeview.State);
					text.Weight = (int)Pango.Weight.Bold;
				}
			}
			else
			{
				text.ForegroundGdk = d_treeview.Style.Foreground(d_treeview.State);
			}
			
			if (fromtemp)
			{
				text.Style = Pango.Style.Italic;
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

		private CellRenderer NextCell(CellRenderer renderer, TreePath path, bool prev, out TreeViewColumn column, out TreePath nextPath)
		{
			TreeViewColumn[] columns = d_treeview.Columns;
			bool getnext = false;
			CellRenderer prevedit = null;
			column = null;
			
			nextPath = path.Copy();

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
							if (prevedit == null && nextPath.Prev())
							{
								column = columns[columns.Length - 1];
								prevedit = column.CellRenderers[column.CellRenderers.Length - 1];
							}

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
			
			nextPath.Next();
			
			if (nextPath.Indices[0] < d_treeview.NodeStore.Count)
			{
				column = columns[0];
				return column.CellRenderers[0];
			}
			else
			{
				return null;
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
				
				handler(d_editingEntry.Text, d_editingPath);
				renderer.StopEditing(false);
				
				TreePath nextPath;

				/* Start editing the next cell */
				CellRenderer next = NextCell(renderer, new TreePath(d_editingPath), (args.Event.State & Gdk.ModifierType.ShiftMask) != 0, out column, out nextPath);
				
				if (next != null)
				{
					d_treeview.SetCursorOnCell(nextPath, column, next, true);
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

	}
}

