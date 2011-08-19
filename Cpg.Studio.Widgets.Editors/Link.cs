using System;
using Gtk;
using System.Collections.Generic;

namespace Cpg.Studio.Widgets.Editors
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.KP_Subtract, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding"),
	 Gtk.Binding(Gdk.Key.KP_Add, "HandleAddBinding")]
	public class Link : ScrolledWindow
	{
		private class Node : Widgets.Node
		{
			private LinkAction d_action;

			public Node(LinkAction action)
			{
				d_action = action;
				
				d_action.AddNotification("target", OnActionChanged);
				d_action.AddNotification("equation", OnActionChanged);
			}
			
			public override void Dispose()
			{
				d_action.RemoveNotification("target", OnActionChanged);
				d_action.RemoveNotification("equation", OnActionChanged);
				
				base.Dispose();
			}
			
			private void OnActionChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[PrimaryKey]
			public LinkAction LinkAction
			{
				get	{ return d_action; }
			}
			
			[NodeColumn(0)]
			public string Target
			{
				get { return d_action.Target; }
			}
			
			[NodeColumn(1)]
			public string Equation
			{
				get { return d_action.Equation.AsString; }
			}
		}

		private Wrappers.Link d_link;
		private Actions d_actions;
		private Widgets.TreeView<Node> d_treeview;
		private bool d_selectAction;

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
			
			Add(d_treeview);
			
			CellRendererText renderer;
			Gtk.TreeViewColumn column;
			
			// Target renderer
			
			renderer = new CellRendererText();
			column = d_treeview.AppendColumn("Target", renderer, "text", 0);
			
			column.SetCellDataFunc(renderer, VisualizeProperties);

			renderer.Editable = true;
			renderer.Edited += HandleLinkActionTargetEdited;
			renderer.EditingStarted += HandleLinkActionTargetEditingStarted;

			column.MinWidth = 80;
			
			// Equation renderer
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			renderer.Edited += HandleLinkActionEquationEdited;
			
			column = d_treeview.AppendColumn("Equation", renderer, "text", 1);
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
			
			if (d_link.To == null)
			{
				return;
			}
			
			EntryCompletion completion = new EntryCompletion();
			ListStore props = new ListStore(typeof(string));
			Dictionary<string, bool> found = new Dictionary<string, bool>();

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

			foreach (Cpg.LinkAction action in d_link.Actions)
			{
				AddLinkAction(action);
			}
		}
		
		private void HandleLinkActionRemoved(object source, Cpg.LinkAction action)
		{
			d_treeview.NodeStore.Remove(action);
		}

		private void HandleLinkActionAdded(object source, Cpg.LinkAction action)
		{
			AddLinkAction(action);
		}
		
		private void AddLinkAction(Cpg.LinkAction action)
		{
			TreeIter iter;
			
			d_treeview.NodeStore.Add(new Node(action), out iter);
			
			if (d_selectAction)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
				
				TreePath path = d_treeview.NodeStore.GetPath(iter);					
				d_treeview.SetCursor(path, d_treeview.GetColumn(0), true);
			}			
		}

		private void HandleLinkActionTargetEdited(object o, EditedArgs args)
		{
			Node node = d_treeview.NodeStore.FindPath(args.Path);
			
			if (node.LinkAction.Target == args.NewText.Trim())
			{
				return;
			}

			d_actions.Do(new Undo.ModifyLinkActionTarget(d_link, node.LinkAction.Target, args.NewText.Trim()));
		}
		
		private void HandleLinkActionEquationEdited(object o, EditedArgs args)
		{
			Node node = d_treeview.NodeStore.FindPath(args.Path);
			
			if (node.LinkAction.Equation.AsString == args.NewText.Trim())
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyLinkActionEquation(d_link, node.LinkAction.Target, args.NewText.Trim()));
		}

		private void DoAddAction()
		{
			List<string> props = new List<string>(Array.ConvertAll<LinkAction, string>(d_link.Actions, item => item.Target));
			List<string> prefs = new List<string>();
			
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
		}
		
		private void DoRemoveAction()
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				Node node = d_treeview.NodeStore.FindPath(path);

				actions.Add(new Undo.RemoveLinkAction(d_link, node.LinkAction));
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
			item.AccelPath = "<CpgStudio>/Widgets/Editors/Properties/Add";
			
			AccelMap.AddEntry("<CpgStudio>/Widgets/Editors/Properties/Add", (uint)Gdk.Key.KP_Add, Gdk.ModifierType.None);

			item.Show();
			item.Activated += delegate { DoAddAction(); };
			
			menu.Append(item);

			item = new MenuItem("Remove");
			item.AccelPath = "<CpgStudio>/Widgets/Editors/Properties/Remove";
			item.Show();
			
			AccelMap.AddEntry("<CpgStudio>/Widgets/Editors/Properties/Remove", (uint)Gdk.Key.KP_Subtract, Gdk.ModifierType.None);
			
			item.Sensitive = (d_treeview.Selection.CountSelectedRows() > 0);
			item.Activated += delegate { DoRemoveAction(); };
			
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

			if (args.Event.Type != Gdk.EventType.TwoButtonPress && args.Event.Type != Gdk.EventType.ThreeButtonPress)
			{
				return;
			}
			
			if (args.Event.Window != d_treeview.BinWindow)
			{
				return;
			}
			
			TreePath path;
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
			
			bool fromtemp = (d_link.GetActionTemplate(node.LinkAction, true) != null);
			bool overridden = (d_link.GetActionTemplate(node.LinkAction, false) != null);
			
			text.Weight = (int)Pango.Weight.Normal;
			text.Style = Pango.Style.Normal;
			
			if (d_link.To == null || d_link.To.Property(node.Target) == null)
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
	}
}

