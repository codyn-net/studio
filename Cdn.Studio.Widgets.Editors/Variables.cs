using System;
using Gtk;
using System.Collections.Generic;

namespace Cdn.Studio.Widgets.Editors
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.KP_Subtract, "HandleDeleteBinding"),
	 Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding"),
	 Gtk.Binding(Gdk.Key.KP_Add, "HandleAddBinding")]
	public class Variables : Gtk.ScrolledWindow
	{
		private class Node : Widgets.Node
		{
			private Cdn.Variable d_variable;
			
			public enum Columns
			{
				Name = 0,
				Expression = 1,
				Integrated = 2,
				Flags = 3,
				Editable = 4,
				Target = 5,
				Tooltip = 6,
				Style = 7
			}
			
			public Node(Cdn.Variable property)
			{
				d_variable = property;
				
				InstallMonitoring();
			}
			
			public Node() : this(null)
			{
			}
			
			public override void Dispose()
			{
				UninstallMonitoring();
				base.Dispose();
			}
			
			private void UninstallMonitoring()
			{
				if (d_variable == null)
				{
					return;
				}
				
				d_variable.RemoveNotification("name", OnPropertyChanged);
				d_variable.RemoveNotification("expression", OnPropertyChanged);
				d_variable.RemoveNotification("flags", OnPropertyChanged);
			}
			
			private void InstallMonitoring()
			{
				if (d_variable == null)
				{
					return;
				}
	
				d_variable.AddNotification("name", OnPropertyChanged);
				d_variable.AddNotification("expression", OnPropertyChanged);
				d_variable.AddNotification("flags", OnPropertyChanged);
			}
			
			private void OnPropertyChanged(object o, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[PrimaryKey]
			public Cdn.Variable Variable
			{
				get
				{
					return d_variable;
				}
				set
				{
					if (d_variable != value)
					{
						UninstallMonitoring();
	
						d_variable = value;
						
						InstallMonitoring();
	
						EmitChanged();
					}
				}
			}
			
			[NodeColumn(Columns.Name), PrimaryKey]
			public virtual string Name
			{
				get
				{
					return d_variable != null ? d_variable.Name : "Add...";
				}
			}
			
			[NodeColumn(Columns.Expression)]
			public virtual string Expression
			{
				get
				{
					return d_variable != null && d_variable.Expression != null ? d_variable.Expression.AsString : "";
				}
			}
			
			[NodeColumn(Columns.Integrated)]
			public virtual bool Integrated
			{
				get
				{
					return d_variable != null ? d_variable.Integrated : false;
				}
			}
			
			[NodeColumn(Columns.Flags)]
			public string Flags
			{
				get
				{
					if (d_variable == null)
					{
						return "";
					}
	
					// Ignore integrated
					VariableFlags filt = d_variable.Flags & ~VariableFlags.Integrated;
	
					return Variable.FlagsToString(filt, 0);
				}
			}
			
			[NodeColumn(Columns.Editable)]
			public virtual bool Editable
			{
				get
				{
					return d_variable != null;
				}
			}
			
			[NodeColumn(Columns.Target)]
			public virtual string Target
			{
				get
				{
					return "";
				}
			}
			
			[NodeColumn(Columns.Tooltip)]
			public virtual string Tooltip
			{
				get
				{
					if (d_variable == null)
					{
						return null;
					}

					Cdn.Object templ = d_variable.Object.GetVariableTemplate(d_variable, false);
					List<string > parts = new List<string>();

					string annotation = d_variable.Annotation;
						
					if (annotation != null)
					{
						parts.Add(annotation.Replace("\n", " "));
					}
						
					if (templ != null)
					{
						parts.Add(String.Format("<i>From: <tt>{0}</tt></i>", templ.FullIdForDisplay));
					}

					if (d_variable.Expression.Instructions.Length != 0)
					{
						parts.Add(String.Format("<i>Value: <tt>{0}</tt></i>", d_variable.Value));

						ExpressionTreeIter it = new ExpressionTreeIter(d_variable.Expression);
						it.Simplify();

						string its = it.ToStringDbg();

						if (its.Length >= 203)
						{
							its = its.Substring(0, 200) + "...";
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
		}
		
		private class InterfaceNode : Node
		{
			private Cdn.VariableInterface d_iface;
			private string d_name;
	
			public InterfaceNode(Cdn.VariableInterface iface, string name)
			{
				d_iface = iface;
				d_name = name;
				
				Variable = iface.Lookup(name);
				
				InstallMonitoring();
			}
			
			public override void Dispose()
			{
				UninstallMonitoring();
				base.Dispose();
			}
			
			public override string Name
			{
				get
				{
					return d_name;
				}
			}
			
			public string ChildName
			{
				get
				{
					return d_iface.LookupChildName(d_name);
				}
			}
			
			public string VariableName
			{
				get
				{
					return d_iface.LookupVariableName(d_name);
				}
			}
			
			public override string Target
			{
				get
				{
					return ChildName + "." + VariableName;
				}
			}
			
			private void UninstallMonitoring()
			{
				d_iface.Node.ChildAdded -= OnChildAdded;
				d_iface.Node.ChildRemoved -= OnChildRemoved;
				d_iface.Node.RemoveNotification("proxy", OnProxyChanged);
			}
			
			private void InstallMonitoring()
			{
				d_iface.Node.ChildAdded += OnChildAdded;
				d_iface.Node.ChildRemoved += OnChildRemoved;
				d_iface.Node.AddNotification("proxy", OnProxyChanged);
			}
	
			private void OnChildAdded(object o, ChildAddedArgs args)
			{
				Variable = d_iface.Lookup(d_name);
			}
			
			private void OnChildRemoved(object o, ChildRemovedArgs args)
			{
				Variable = d_iface.Lookup(d_name);
			}
			
			private void OnProxyChanged(object o, GLib.NotifyArgs args)
			{
				Variable = d_iface.Lookup(d_name);
			}
		}

		public delegate void ErrorHandler(object source,Exception exception);

		public event ErrorHandler Error = delegate {};

		private Wrappers.Wrapper d_wrapper;
		private Actions d_actions;
		private TreeView<Node> d_treeview;
		private ListStore d_flagsStore;
		private List<KeyValuePair<string, Cdn.VariableFlags>> d_flaglist;
		private Entry d_editingEntry;
		private string d_editingPath;
		private bool d_selectProperty;
		private bool d_blockInterfaceRemove;
		private Node d_dummy;

		public Variables(Wrappers.Wrapper wrapper, Actions actions)
		{
			d_wrapper = wrapper;
			d_actions = actions;
			d_blockInterfaceRemove = false;
			
			WidgetFlags |= WidgetFlags.NoWindow;
			
			Build();
		}
		
		protected override void OnDestroyed()
		{
			Disconnect();
			base.OnDestroyed();
		}
		
		private void DoEditingStarted(object o, EditingStartedArgs args)
		{
			d_editingPath = args.Path;

			FillFlagsStore(d_treeview.NodeStore.FindPath(args.Path).Variable);
		}

		private void FillFlagsStore(Cdn.Variable property)
		{
			d_flagsStore.Clear();

			Cdn.VariableFlags flags = property.Flags;
			Cdn.VariableFlags building = Cdn.VariableFlags.None;
			
			List<string > items = new List<string>();
			List<KeyValuePair<string, Cdn.VariableFlags>> copy = new List<KeyValuePair<string, Cdn.VariableFlags>>(d_flaglist);
			
			copy.Reverse();
			
			foreach (KeyValuePair<string, Cdn.VariableFlags> pair in copy)
			{
				string name = pair.Key;
				
				if ((flags & pair.Value) == pair.Value &&
				    (building & pair.Value) == 0)
				{
					building |= pair.Value;
					name = "• " + name;
				}
				
				items.Add(name);
			}
			
			items.Reverse();
			
			foreach (string s in items)
			{
				d_flagsStore.AppendValues(s);
			}
		}

		private void InitializeFlagsList()
		{
			d_flaglist = new List<KeyValuePair<string, Cdn.VariableFlags>>();
			Type type = typeof(Cdn.VariableFlags);

			Array values = Enum.GetValues(type);
			
			for (int i = 0; i < values.Length; ++i)
			{
				Cdn.VariableFlags flags = (Cdn.VariableFlags)values.GetValue(i);
				
				// Don't show 'None' and Integrated is handled separately
				if ((int)flags != 0 && flags != VariableFlags.Integrated)
				{
					d_flaglist.Add(new KeyValuePair<string, Cdn.VariableFlags>(Variable.FlagsToString(flags, 0), flags));
				}
			}
		}
		
		private void Build()
		{
			SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			ShadowType = ShadowType.EtchedIn;
			
			d_treeview = new TreeView<Node>();
			d_treeview.Show();
			
			d_treeview.EnableSearch = false;
			
			d_treeview.RulesHint = true;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			d_treeview.ShowExpanders = false;

			d_treeview.ButtonPressEvent += OnTreeViewButtonPressEvent;
			d_treeview.KeyPressEvent += OnTreeViewKeyPressEvent;
			
			d_treeview.TooltipColumn = (int)Node.Columns.Tooltip;
			
			CellRendererText renderer;
			TreeViewColumn column;
			
			// Setup renderer for the name of the property
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			column = d_treeview.AppendColumn("Name", renderer, "text", Node.Columns.Name);
			column.Resizable = true;
			column.MinWidth = 75;
			
			column.SetCellDataFunc(renderer, VisualizeProperties);
			
			CellRenderer rname = renderer;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				d_editingEntry = args.Editable as Entry;
				d_editingPath = args.Path;
				
				Node node = d_treeview.NodeStore.FindPath(new TreePath(args.Path));
				
				if (node.Variable == null && !(node is InterfaceNode))
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
			
			renderer.Edited += DoNameEdited;
			
			// Setup renderer for expression of the property
			renderer = new Gtk.CellRendererText();
			column = d_treeview.AppendColumn("Expression", renderer,
			                                 "text", Node.Columns.Expression,
			                                 "editable", Node.Columns.Editable);
			
			column.Resizable = true;
			column.Expand = true;
			
			column.SetCellDataFunc(renderer, VisualizeProperties);
			
			CellRenderer rexpr = renderer;
			
			renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
				d_editingEntry = args.Editable as Entry;
				d_editingPath = args.Path;
				
				d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
				{
					OnEntryKeyPressed(a, rexpr, ExpressionEdited);
				};
			};

			renderer.EditingCanceled += delegate(object sender, EventArgs e) {
				if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
				{
					// Still do it actually
					ExpressionEdited(d_editingEntry.Text, d_editingPath);
				}
			};

			renderer.Edited += DoExpressionEdited;

			// Setup renderer for integrated toggle
			CellRendererToggle toggle;
			
			toggle = new Gtk.CellRendererToggle();
			column = d_treeview.AppendColumn(" ∫", toggle,
			                                 "active", Node.Columns.Integrated,
			                                 "activatable", Node.Columns.Editable);
			column.Resizable = false;
			
			toggle.Toggled += DoIntegratedToggled;
			
			// Setup renderer for flags
			CellRendererCombo combo;
			combo = new Gtk.CellRendererCombo();

			column = d_treeview.AppendColumn("Flags", combo,
			                                 "text", Node.Columns.Flags,
			                                 "editable", Node.Columns.Editable);
			column.Resizable = true;
			column.Expand = false;
			
			combo.EditingStarted += DoEditingStarted;
			combo.Edited += DoFlagsEdited;

			combo.HasEntry = false;
			
			d_flagsStore = new ListStore(typeof(string));
			combo.Model = d_flagsStore;
			combo.TextColumn = 0;
			
			column.MinWidth = 50;
			
			if (d_wrapper != null && d_wrapper is Wrappers.Node)
			{
				renderer = new Gtk.CellRendererText();

				column = d_treeview.AppendColumn("Interface", renderer,
				                                 "text", Node.Columns.Target);

				renderer.Editable = true;

				column.SetCellDataFunc(renderer, VisualizeProperties);
				
				CellRenderer riface = renderer;
				
				renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
					d_editingEntry = args.Editable as Entry;
					d_editingPath = args.Path;
					
					d_editingEntry.KeyPressEvent += delegate (object source, KeyPressEventArgs a)
					{
						OnEntryKeyPressed(a, riface, InterfaceEdited);
					};
				};
				
				renderer.EditingCanceled += delegate(object sender, EventArgs e) {
					if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
					{
						// Still do it actually
						InterfaceEdited(d_editingEntry.Text, d_editingPath);
					}
				};

				renderer.Edited += DoInterfaceEdited;
			}
			
			Populate();
			InitializeFlagsList();
			
			Add(d_treeview);
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
		
		private delegate void EditedHandler(string text,string path);
		
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
		
		private void Populate()
		{
			if (d_wrapper == null)
			{
				return;
			}

			foreach (Cdn.Variable prop in d_wrapper.Variables)
			{
				d_treeview.NodeStore.Add(new Node(prop));
			}
			
			Wrappers.Node grp = d_wrapper as Wrappers.Node;

			if (grp != null)
			{
				foreach (string name in grp.VariableInterface.Names)
				{
					d_treeview.NodeStore.Add(new InterfaceNode(grp.VariableInterface, name));
				}
			}
			
			d_dummy = new Node(null);
			d_treeview.NodeStore.Add(d_dummy);
			
			Connect();
		}

		private void VisualizeProperties(TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			CellRendererText text = renderer as CellRendererText;
			Node node = d_treeview.NodeStore.GetFromIter(iter);
			
			bool fromtemp = false;
			bool overridden = false;
			
			if (node.Variable != null)
			{
				fromtemp = (node.Variable.Object.GetVariableTemplate(node.Variable, true) != null);
				overridden = (node.Variable.Object.GetVariableTemplate(node.Variable, false) != null);
			}
			
			text.Weight = (int)Pango.Weight.Normal;
			text.Style = Pango.Style.Normal;
			
			if (node.Variable == null)
			{
				if (node is InterfaceNode)
				{
					text.Foreground = "#ff0000";
				}
				else
				{
					text.ForegroundGdk = d_treeview.Style.Foreground(Gtk.StateType.Insensitive);
					text.Style = Pango.Style.Italic;
				}
			}
			else if (fromtemp || overridden)
			{
				text.ForegroundGdk = d_treeview.Style.Foreground(Gtk.StateType.Insensitive);
				
				if (!fromtemp && overridden && col.Title == "Expression")
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

		private bool ObjectIsNetwork
		{
			get
			{
				return d_wrapper != null && d_wrapper is Wrappers.Network;
			}
		}

		private void Disconnect()
		{
			if (d_wrapper == null)
			{
				return;
			}
			
			d_wrapper.VariableAdded -= DoVariableAdded;
			d_wrapper.VariableRemoved -= DoVariableRemoved;
			
			if (d_wrapper is Wrappers.Node && !ObjectIsNetwork)
			{
				Cdn.VariableInterface iface = (d_wrapper as Wrappers.Node).VariableInterface;
				
				iface.Added -= HandleGroupInterfaceVariableAdded;
				iface.Removed -= HandleGroupInterfaceVariableRemoved;
			}
		}
		
		private void HandleGroupInterfaceVariableAdded(object source, Cdn.AddedArgs args)
		{
			Wrappers.Node grp = (Wrappers.Node)d_wrapper;
			
			if (!d_blockInterfaceRemove)
			{
				d_treeview.NodeStore.Add(new InterfaceNode(grp.VariableInterface, args.Name));
			}
			else
			{
				Node node = d_treeview.NodeStore.Find(args.Name);
				
				if (node != null)
				{
					node.EmitChanged();
				}
			}
		}
		
		private void HandleGroupInterfaceVariableRemoved(object source, Cdn.RemovedArgs args)
		{
			if (!d_blockInterfaceRemove)
			{
				d_treeview.NodeStore.Remove(args.Name);
			}
		}
		
		private void Connect()
		{
			if (d_wrapper == null)
			{
				return;
			}
			
			d_wrapper.VariableAdded += DoVariableAdded;
			d_wrapper.VariableRemoved += DoVariableRemoved;
			
			if (d_wrapper is Wrappers.Node && !ObjectIsNetwork)
			{
				Cdn.VariableInterface iface = (d_wrapper as Wrappers.Node).VariableInterface;
				
				iface.Added += HandleGroupInterfaceVariableAdded;
				iface.Removed += HandleGroupInterfaceVariableRemoved;
			}
		}

		private void DoIntegratedToggled(object source, ToggledArgs args)
		{
			Node node = d_treeview.NodeStore.FindPath(args.Path);
			VariableFlags flags = node.Variable.Flags;
			CellRendererToggle toggle = (CellRendererToggle)source;
			
			if (!toggle.Active)
			{
				flags |= VariableFlags.Integrated;
			}
			else
			{
				flags &= ~VariableFlags.Integrated;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_wrapper, node.Variable, flags));
		}
		
		private void DoFlagsEdited(object source, EditedArgs args)
		{
			Node node = d_treeview.NodeStore.FindPath(args.Path);

			bool wason = false;
			string name = args.NewText;
			
			if (String.IsNullOrEmpty(name))
			{
				return;
			}
			
			if (name.StartsWith("• "))
			{
				wason = true;
				name = name.Substring(2);
			}

			Cdn.VariableFlags add_flags;
			Cdn.VariableFlags remove_flags;
			
			Variable.FlagsFromString(name, out add_flags, out remove_flags);
			Cdn.VariableFlags newflags = node.Variable.Flags;

			if (wason)
			{
				newflags &= ~add_flags;
			}
			else
			{
				newflags |= add_flags;
			}
			
			if (newflags == node.Variable.Flags)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_wrapper, node.Variable, newflags));
		}
		
		private void ExpressionEdited(string newValue, string path)
		{
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			if (newValue.Trim() == node.Variable.Expression.AsString)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_wrapper, node.Variable, newValue.Trim()));
		}
		
		private void DoExpressionEdited(object source, EditedArgs args)
		{
			ExpressionEdited(args.NewText, args.Path);
		}
		
		private bool ParseInterface(string iface, out string child, out string prop)
		{
			string[] parts;
					
			parts = iface.Split(new char[] {'.'}, 2);
			
			child = "";
			prop = "";
			
			if (parts.Length != 2 || parts[0].Trim() == "" || parts[1].Trim() == "")
			{
				Error(this, new Exception("Invalid interface target, specify <child>.<property>"));
				return false;
			}
			
			child = parts[0].Trim();
			prop = parts[1].Trim();

			return true;
		}
		
		private void InterfaceEdited(string newValue, string path)
		{
			Node node = d_treeview.NodeStore.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			string val = newValue.Trim();
					
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			if (node is InterfaceNode)
			{
				InterfaceNode n = (InterfaceNode)node;
				
				if (n.Target == val)
				{
					return;
				}

				if (val == "")
				{
					// Remove interface
					actions.Add(new Undo.RemoveInterfaceVariable((Wrappers.Node)d_wrapper, n.Name, n.ChildName, n.VariableName));
					
					string expr = "0";
					Cdn.VariableFlags flags = Cdn.VariableFlags.None;
					
					if (node.Variable != null)
					{
						expr = node.Variable.Expression.AsString;
						flags = node.Variable.Flags;
					}

					// Add normal property instead
					actions.Add(new Undo.AddVariable(d_wrapper, n.Name, expr, flags));
				}
				else
				{
					string child;
					string prop;

					if (!ParseInterface(val, out child, out prop))
					{
						return;
					}

					d_blockInterfaceRemove = true;

					actions.Add(new Undo.RemoveInterfaceVariable((Wrappers.Node)d_wrapper, n.Name, n.ChildName, n.VariableName));
					actions.Add(new Undo.AddInterfaceProperty((Wrappers.Node)d_wrapper, n.Name, child, prop));
				}
			}
			else if (val != "")
			{
				string child;
				string prop;

				if (!ParseInterface(val, out child, out prop))
				{
					return;
				}

				actions.Add(new Undo.RemoveVariable(d_wrapper, node.Variable));
				actions.Add(new Undo.AddInterfaceProperty((Wrappers.Node)d_wrapper, node.Variable.Name, child, prop));
			}
			else
			{
				return;
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
			
			d_blockInterfaceRemove = false;
		}
		
		private void DoInterfaceEdited(object source, EditedArgs args)
		{
			InterfaceEdited(args.NewText, args.Path);
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
			
			if (!(node is InterfaceNode) && node.Variable == null)
			{
				/* Add a new property */
				try
				{
					d_actions.Do(new Undo.AddVariable(d_wrapper, newName.Trim(), "", VariableFlags.None));
				}
				catch (GLib.GException err)
				{
					// Display could not remove, or something
					Error(this, err);
				}
				
				return;
			}
			
			if (newName.Trim() == node.Variable.Name)
			{
				return;
			}

			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			if (node is InterfaceNode)
			{
				InterfaceNode n = (InterfaceNode)node;

				actions.Add(new Undo.RemoveInterfaceVariable((Wrappers.Node)d_wrapper, n.Name, n.ChildName, n.VariableName));
				actions.Add(new Undo.AddInterfaceProperty((Wrappers.Node)d_wrapper, newName.Trim(), n.ChildName, n.VariableName));
			}
			else
			{
				actions.Add(new Undo.RemoveVariable(d_wrapper, node.Variable));
				actions.Add(new Undo.AddVariable(d_wrapper, newName.Trim(), node.Variable.Expression.AsString, node.Variable.Flags));
			}
			
			try
			{
				d_actions.Do(new Undo.Group(actions));
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

		private void DoAddProperty(object source, EventArgs args)
		{
			DoAddProperty();
		}
		
		private bool PropertyExists(string name)
		{
			if (d_wrapper.Variable(name) != null)
			{
				return true;
			}
			
			Wrappers.Node grp = d_wrapper as Wrappers.Node;
			
			if (grp == null)
			{
				return false;
			}
			
			return grp.VariableInterface.Implements(name);
		}
		
		private void DoAddProperty()
		{
			int num = 1;
			
			while (PropertyExists("x" + num))
			{
				++num;
			}
			
			d_selectProperty = true;
			d_actions.Do(new Undo.AddVariable(d_wrapper, "x" + num, "0", Cdn.VariableFlags.None));
			d_selectProperty = false;
		}
		
		private void DoRemoveProperty(object source, EventArgs args)
		{
			DoRemoveProperty();
		}
		
		private void DoRemoveProperty()
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
			{
				Node node = d_treeview.NodeStore.FindPath(path);
				
				if (node == d_dummy)
				{
					continue;
				}
				
				Wrappers.Wrapper temp = d_wrapper.GetVariableTemplate(node.Variable, false);
				
				if (temp != null)
				{
					Cdn.Variable tempProp = temp.Variable(node.Variable.Name);

					actions.Add(new Undo.ModifyProperty(d_wrapper, node.Variable, tempProp.Expression.AsString));
					actions.Add(new Undo.ModifyProperty(d_wrapper, node.Variable, tempProp.Flags));
				}
				else
				{
					actions.Add(new Undo.RemoveVariable(d_wrapper, node.Variable));
				}
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
		
		private void DoVariableAdded(Wrappers.Wrapper obj, Cdn.Variable prop)
		{
			Node node = new Node(prop);

			d_treeview.NodeStore.Remove(d_dummy);
			d_treeview.NodeStore.Add(node);
			d_treeview.NodeStore.Add(d_dummy);
			
			if (d_selectProperty)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectPath(node.Path);
				
				d_treeview.SetCursor(node.Path, d_treeview.GetColumn(0), true);
			}
		}
		
		private void DoVariableRemoved(Wrappers.Wrapper obj, Cdn.Variable prop)
		{
			d_treeview.NodeStore.Remove(prop);
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
			item.Activated += DoAddProperty;
			
			menu.Append(item);

			item = new MenuItem("Remove");
			item.AccelPath = "<CdnStudio>/Widgets/Editors/Properties/Remove";
			item.Show();
			
			AccelMap.AddEntry("<CdnStudio>/Widgets/Editors/Properties/Remove", (uint)Gdk.Key.KP_Subtract, Gdk.ModifierType.None);
			
			item.Sensitive = (d_treeview.Selection.CountSelectedRows() > 0);
			item.Activated += DoRemoveProperty;
			
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
		
		private void HandleAddBinding()
		{
			DoAddProperty();
		}
		
		private void HandleDeleteBinding()
		{
			DoRemoveProperty();
		}
	}
}

