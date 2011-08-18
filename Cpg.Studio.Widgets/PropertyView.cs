using System;
using Gtk;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Widgets
{
	[Gtk.Binding(Gdk.Key.Delete, "HandleDeleteBinding")]
	[Gtk.Binding(Gdk.Key.Insert, "HandleAddBinding")]
	public class PropertyView : VBox
	{
		private class InterfacePropertyNode : Node
		{
			private Cpg.PropertyInterface d_iface;
			private string d_name;
			
			public InterfacePropertyNode(Cpg.PropertyInterface iface, string name)
			{
				d_iface = iface;
				d_name = name;
			}
			
			public string ChildName
			{
				get
				{
					return d_iface.LookupChildName(d_name);
				}
			}
			
			public string PropertyName
			{
				get
				{
					return d_iface.LookupPropertyName(d_name);
				}
			}
			
			[PrimaryKey, NodeColumn(0)]
			public string Name
			{
				get	{ return d_name; }
			}
			
			[NodeColumn(1)]
			public string Target
			{
				get
				{
					return ChildName + "." + PropertyName;
				}
			}
		}

		private class LinkActionNode : Node
		{
			private LinkAction d_action;

			public LinkActionNode(LinkAction action)
			{
				d_action = action;
				
				d_action.AddNotification("target", OnActionChanged);
				d_action.AddNotification("equation", OnActionChanged);
			}
			
			~LinkActionNode()
			{
				d_action.RemoveNotification("target", OnActionChanged);
				d_action.RemoveNotification("equation", OnActionChanged);
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
		
		private class PropertyNode : Node
		{
			private Property d_property;

			public PropertyNode(Property property)
			{
				d_property = property;
				
				d_property.AddNotification("name", OnPropertyChanged);
				d_property.AddNotification("expression", OnPropertyChanged);
				d_property.AddNotification("flags", OnPropertyChanged);
			}
			
			~PropertyNode()
			{
				d_property.RemoveNotification("name", OnPropertyChanged);
				d_property.RemoveNotification("expression", OnPropertyChanged);
				d_property.RemoveNotification("flags", OnPropertyChanged);
			}
			
			private void OnPropertyChanged(object source, GLib.NotifyArgs args)
			{
				EmitChanged();
			}
			
			[PrimaryKey]
			public Property Property
			{
				get	{ return d_property; }
			}
			
			[NodeColumn(0), PrimaryKey]
			public string Name
			{
				get { return d_property.Name; }
			}
			
			[NodeColumn(1)]
			public string Expression
			{
				get { return d_property.Expression.AsString; }
			}
			
			[NodeColumn(2)]
			public bool Integrated
			{
				
				get { return (d_property.Flags & PropertyFlags.Integrated) != 0; }
			}
			
			[NodeColumn(3)]
			public string Flags
			{
				get 
				{
					// Ignore integrated
					PropertyFlags filt = d_property.Flags & ~PropertyFlags.Integrated;
					return Property.FlagsToString(filt, 0);
				}
			}
		}

		enum Column
		{
			Property = 0
		}

		public delegate void ErrorHandler(object source, Exception exception);
		public delegate void TemplateHandler(object source, Wrappers.Wrapper template);
		
		public event ErrorHandler Error = delegate {};
		public event TemplateHandler TemplateActivated = delegate {};
		
		private Wrappers.Wrapper d_object;
		private NodeStore<PropertyNode> d_store;
		private TreeView d_treeview;
		private bool d_selectProperty;
		private NodeStore<LinkActionNode> d_actionStore;
		private TreeView d_actionView;
		private ListStore d_flagsStore;
		private List<KeyValuePair<string, Cpg.PropertyFlags>> d_flaglist;
		private Actions d_actions;
		private AddRemovePopup d_propertyControls;
		private ListStore d_proxyStore;
		private ComboBox d_proxyCombo;
		private AddRemovePopup d_actionControls;
		private bool d_selectAction;
		private HBox d_extraControl;
		private HPaned d_paned;
		private Entry d_entry;
		private HBox d_templateParent;
		private Entry d_editingEntry;
		private string d_editingPath;
		private FileChooserButton d_inputFileChooser;
		private Dialogs.FindTemplate d_findTemplate;
		private Wrappers.Network d_network;
		
		private NodeStore<InterfacePropertyNode> d_interfacePropertyStore;
		private TreeView d_interfacePropertyView;
		private AddRemovePopup d_interfacePropertyControls;
		private bool d_selectInterface;
		
		public PropertyView(Wrappers.Network network, Actions actions, Wrappers.Wrapper obj) : base(false, 3)
		{
			d_network = network;
			d_selectProperty = false;
			d_selectAction = false;
			d_selectInterface = false;

			d_actions = actions;

			Initialize(obj, true);
		}
		
		public Wrappers.Wrapper Object
		{
			get
			{
				return d_object;
			}
		}
		
		public PropertyView(Wrappers.Network network, Actions actions) : this(network, actions, null)
		{
		}
		
		private void AddEquationsUI()
		{
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			d_paned.Add2(vbox);
			
			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_actionStore = new NodeStore<LinkActionNode>();
			d_actionView = new TreeView(new TreeModelAdapter(d_actionStore));
			
			d_actionView.ShowExpanders = false;
			d_actionView.RulesHint = true;
			d_actionView.Selection.Mode = SelectionMode.Multiple;
			d_actionView.ButtonPressEvent += OnTreeViewButtonPressEvent;
			
			vw.Add(d_actionView);
			
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;			
			renderer.Edited += HandleLinkActionTargetEdited;

			Gtk.TreeViewColumn column = new Gtk.TreeViewColumn("Target", renderer, "text", 0);
			column.MinWidth = 80;
			
			d_actionView.AppendColumn(column);
			
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			renderer.Edited += HandleLinkActionEquationEdited;
			
			column = new Gtk.TreeViewColumn("Equation", renderer, "text", 1);
			d_actionView.AppendColumn(column);
			
			vbox.PackStart(vw, true, true, 0);

			d_actionControls = new AddRemovePopup(d_actionView);
			d_actionControls.AddButton.Clicked += DoAddAction;
			d_actionControls.RemoveButton.Clicked += DoRemoveAction;
			
			UpdateActionSensitivity();
			
			Wrappers.Link link = d_object as Wrappers.Link;
			
			foreach (Cpg.LinkAction action in link.Actions)
			{
				AddLinkAction(action);
			}
			
			d_actionView.Selection.Changed += DoActionSelectionChanged;
		}

		
		private void HandleLinkActionTargetEdited(object o, EditedArgs args)
		{
			LinkActionNode node = d_actionStore.FindPath(args.Path);
			
			if (node.LinkAction.Target == args.NewText.Trim())
			{
				return;
			}

			d_actions.Do(new Undo.ModifyLinkActionTarget((Wrappers.Link)d_object, node.LinkAction.Target, args.NewText.Trim()));
		}
		
		private void HandleLinkActionEquationEdited(object o, EditedArgs args)
		{
			LinkActionNode node = d_actionStore.FindPath(args.Path);
			
			if (node.LinkAction.Equation.AsString == args.NewText.Trim())
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyLinkActionEquation((Wrappers.Link)d_object, node.LinkAction.Target, args.NewText.Trim()));
		}
		
		private void InitializeFlagsList()
		{
			d_flaglist = new List<KeyValuePair<string, Cpg.PropertyFlags>>();
			Type type = typeof(Cpg.PropertyFlags);

			Array values = Enum.GetValues(type);
			
			for (int i = 0; i < values.Length; ++i)
			{
				Cpg.PropertyFlags flags = (Cpg.PropertyFlags)values.GetValue(i);
				
				// Don't show 'None' and Integrated is handled separately
				if ((int)flags != 0 && flags != PropertyFlags.Integrated)
				{
					d_flaglist.Add(new KeyValuePair<string, Cpg.PropertyFlags>(Property.FlagsToString(flags, 0), flags));
				}
			}
		}
		
		private void AddInputFileUI()
		{
			HBox hbox = new HBox(false, 6);
			hbox.Show();
			
			Label label = new Label("File:");
			label.Show();
			
			hbox.PackStart(label, false, false, 0);
			
			Wrappers.InputFile input = (Wrappers.InputFile)d_object;
			d_inputFileChooser = new FileChooserButton("Open Data File", FileChooserAction.Open);
			
			if (input.WrappedObject.FilePath != null)
			{
				d_inputFileChooser.SetFilename(input.WrappedObject.FilePath);
			}
			
			d_inputFileChooser.SetSizeRequest(300, -1);
			d_inputFileChooser.Show();
			
			d_inputFileChooser.FileSet += DoFileSet;

			hbox.PackStart(d_inputFileChooser, false, false, 0);
			
			Button but = new Button(Gtk.Stock.Clear);
			but.Show();
			
			but.Clicked += DoFileClear;

			hbox.PackStart(but, false, false, 0);
			
			d_extraControl.PackEnd(hbox, false, false, 0);
			
			try
			{
				input.WrappedObject.Ensure();
			}
			catch (GLib.GException e)
			{
				Error(this, e);
			}
		}
		
		private void DoFileClear(object sender, EventArgs args)
		{
			Wrappers.InputFile input = (Wrappers.InputFile)d_object;
			
			input.WrappedObject.FilePath = null;			
			input.WrappedObject.Ensure();
		}

		private void DoFileSet(object sender, EventArgs args)
		{
			Wrappers.InputFile input = (Wrappers.InputFile)d_object;
			
			if (d_inputFileChooser.Filename == null)
			{
				return;
			}

			input.WrappedObject.FilePath = d_inputFileChooser.Filename;
			
			try
			{
				input.WrappedObject.Ensure();
			}
			catch (GLib.GException e)
			{
				Error(this, e);
			}
		}
		
		private void AddGroupInterfaceUI()
		{
			if (d_object is Wrappers.Network)
			{
				return;
			}

			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			d_paned.Add2(vbox);
			
			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_interfacePropertyStore = new NodeStore<InterfacePropertyNode>();
			d_interfacePropertyView = new TreeView(new TreeModelAdapter(d_interfacePropertyStore));
			
			d_interfacePropertyView.ShowExpanders = false;
			d_interfacePropertyView.RulesHint = true;
			d_interfacePropertyView.ButtonPressEvent += OnTreeViewButtonPressEvent;
			
			vw.Add(d_interfacePropertyView);
			
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = true;			
			renderer.Edited += HandleInterfacePropertyNameEdited;

			Gtk.TreeViewColumn column = new Gtk.TreeViewColumn("Interface", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 80;
			
			d_interfacePropertyView.AppendColumn(column);
			
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			renderer.Edited += HandleInterfacePropertyTargetEdited;
			renderer.EditingStarted += HandleInterfacePropertyTargetEditingStarted;
			
			column = new Gtk.TreeViewColumn("Target", renderer, "text", 1);
			d_interfacePropertyView.AppendColumn(column);
			
			vbox.PackStart(vw, true, true, 0);

			d_interfacePropertyControls = new AddRemovePopup(d_interfacePropertyView);
			d_interfacePropertyControls.AddButton.Clicked += DoAddInterfaceProperty;
			d_interfacePropertyControls.RemoveButton.Clicked += DoRemoveInterfaceProperty;
			
			UpdatePropertyInterfaceSensitivity();
			
			Wrappers.Group grp = d_object as Wrappers.Group;
			Cpg.PropertyInterface iface = grp.PropertyInterface;
			
			foreach (string name in iface.Names)
			{
				AddGroupPropertyInterface(name);
			}
			
			d_interfacePropertyView.Selection.Changed += DoInterfacePropertySelectionChanged;
			
			iface.Added += HandleGroupInterfacePropertyAdded;
			iface.Removed += HandleGroupInterfacePropertyRemoved;
		}
		
		private void AddGroupUI()
		{
			AddGroupInterfaceUI();

			HBox hbox = new HBox(false, 6);
			hbox.Show();
			
			Label label = new Label("Proxy:");
			label.Show();
			
			hbox.PackStart(label, false, false, 0);
			
			ListStore store = new ListStore(typeof(string), typeof(Wrappers.Wrapper), typeof(bool));
			Wrappers.Group grp = (Wrappers.Group)d_object;
			List<Wrappers.Wrapper> children = new List<Wrappers.Wrapper>(grp.Children);
			
			children.RemoveAll(item => item is Wrappers.Link);
			children.Sort(delegate (Wrappers.Wrapper a, Wrappers.Wrapper b) {
				return a.Id.CompareTo(b.Id);
			});
			
			ComboBox box = new ComboBox(store);
			TreeIter iter;
			
			iter = store.AppendValues("None", null, false);
			
			if (grp.Proxy == null)
			{
				box.SetActiveIter(iter);	
			}

			store.AppendValues(null, null, true);
			
			foreach (Wrappers.Wrapper child in children)
			{
				iter = store.AppendValues(child.Id, child, false);
				
				if (child == grp.Proxy)
				{
					box.SetActiveIter(iter);
				}
			}

			box.RowSeparatorFunc = delegate (TreeModel model, TreeIter it) {
				return (bool)model.GetValue(it, 2);
			};
			
			box.Changed += OnChangeProxy;
			
			box.Show();

			CellRendererText renderer = new CellRendererText();

			box.PackStart(renderer, true);
			box.AddAttribute(renderer, "text", 0);
			
			hbox.PackStart(box, false, false, 0);
			
			d_extraControl.PackEnd(hbox, false, false, 0);
			
			d_proxyStore = store;
			d_proxyCombo = box;
			
			d_proxyCombo.Sensitive = !ObjectIsNetwork;
		}
		
		private void HandleProxyChanged(object sender, GLib.NotifyArgs args)
		{
			TreeIter iter;

			if (!d_proxyStore.GetIterFirst(out iter))
			{
				return;
			}
			
			Wrappers.Group grp = (Wrappers.Group)d_object;
			
			do
			{
				Wrappers.Wrapper proxy = (Wrappers.Wrapper)d_proxyStore.GetValue(iter, 1);
				
				if (proxy == grp.Proxy)
				{
					d_proxyCombo.Changed -= OnChangeProxy;
					d_proxyCombo.SetActiveIter(iter);
					d_proxyCombo.Changed += OnChangeProxy;
					return;
				}
			} while (d_proxyStore.IterNext(ref iter));
			
			d_proxyCombo.Active = 0;
		}

		private void OnChangeProxy(object sender, EventArgs e)
		{
			Wrappers.Wrapper proxy;
			TreeIter iter;

			if (!d_proxyCombo.GetActiveIter(out iter))
			{
				proxy = null;
			}
			else
			{
				proxy = (Wrappers.Wrapper)d_proxyStore.GetValue(iter, 1);
			}
			
			d_actions.Do(new Undo.ModifyProxy((Wrappers.Group)d_object, proxy));
		}
		
		private void Disconnect()
		{
			if (d_object == null)
			{
				return;
			}
			
			d_object.PropertyAdded -= DoPropertyAdded;
			d_object.PropertyRemoved -= DoPropertyRemoved;
			
			d_object.WrappedObject.RemoveNotification("id", HandleIdChanged);
			
			d_object.TemplateApplied -= HandleTemplateChanged;
			d_object.TemplateUnapplied -= HandleTemplateChanged;
			
			if (d_object is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)d_object;
				grp.WrappedObject.RemoveNotification("proxy", HandleProxyChanged);
				
				if (!(d_object is Wrappers.Network))
				{
					Cpg.PropertyInterface iface = grp.PropertyInterface;
				
					iface.Added -= HandleGroupInterfacePropertyAdded;
					iface.Removed -= HandleGroupInterfacePropertyRemoved;
				}
			}
			else if (d_object is Wrappers.Link)
			{
				Wrappers.Link link = (Wrappers.Link)d_object;
				
				link.ActionAdded -= HandleLinkActionAdded;
				link.ActionRemoved -= HandleLinkActionRemoved;
			}
			else if (d_object is Wrappers.InputFile)
			{
				Wrappers.InputFile input = (Wrappers.InputFile)d_object;
				
				input.WrappedObject.RemoveNotification("file", HandleFileChanged);
			}
		}
		
		private void Connect()
		{
			if (d_object == null)
			{
				return;
			}
			
			d_object.PropertyAdded += DoPropertyAdded;
			d_object.PropertyRemoved += DoPropertyRemoved;
			
			d_object.WrappedObject.AddNotification("id", HandleIdChanged);
			
			d_object.TemplateApplied += HandleTemplateChanged;
			d_object.TemplateUnapplied += HandleTemplateChanged;
			
			if (d_object is Wrappers.Group)
			{
				Wrappers.Group grp = (Wrappers.Group)d_object;
				
				grp.WrappedObject.AddNotification("proxy", HandleProxyChanged);
			}
			else if (d_object is Wrappers.Link)
			{
				Wrappers.Link link = (Wrappers.Link)d_object;
				
				link.ActionAdded += HandleLinkActionAdded;
				link.ActionRemoved += HandleLinkActionRemoved;
			}
			else if (d_object is Wrappers.InputFile)
			{
				Wrappers.InputFile input = (Wrappers.InputFile)d_object;
				
				input.WrappedObject.AddNotification("file", HandleFileChanged);
			}
		}
		
		private void HandleFileChanged(object sender, GLib.NotifyArgs args)
		{
			string path = ((Wrappers.InputFile)d_object).WrappedObject.FilePath;
			
			if (path == null)
			{
				d_inputFileChooser.UnselectAll();
			}
			else
			{
				d_inputFileChooser.SetFilename(path);
			}
		}

		private void HandleTemplateChanged(Wrappers.Wrapper source, Wrappers.Wrapper template)
		{
			RebuildTemplateWidgets();
		}

		private void HandleLinkActionRemoved(object source, Cpg.LinkAction action)
		{
			d_actionStore.Remove(action);
		}

		private void HandleLinkActionAdded(object source, Cpg.LinkAction action)
		{
			AddLinkAction(action);
		}
		
		private void AddLinkAction(Cpg.LinkAction action)
		{
			TreeIter iter;
			
			d_actionStore.Add(new LinkActionNode(action), out iter);
			
			if (d_selectAction)
			{
				d_actionView.Selection.UnselectAll();
				d_actionView.Selection.SelectIter(iter);
				
				TreePath path = d_actionStore.GetPath(iter);					
				d_actionView.SetCursor(path, d_actionView.GetColumn(0), true);
			}			
		}
		
		private void RebuildTemplateWidgets()
		{
			Wrappers.Wrapper[] templates = d_object.AppliedTemplates;
			Widget[] children = d_templateParent.Children;
			
			for (int i = 0; i < children.Length; ++i)
			{
				d_templateParent.Remove(children[i]);
			}

			for (int i = 0; i < templates.Length; ++i)
			{
				Wrappers.Wrapper template = templates[i];

				if (i != 0)
				{
					Label comma = new Label(", ");
					comma.Show();
					d_templateParent.PackStart(comma, false, false, 0);
				}
				
				Label temp = new Label();
				temp.Markup = String.Format("<span underline=\"single\">{0}</span>", System.Security.SecurityElement.Escape(template.FullId));
				
				EventBox box = new EventBox();
				box.Show();
				box.Add(temp);

				temp.StyleSet += HandleTemplateLabelStyleSet;

				box.Realized += delegate(object sender, EventArgs e) {
					box.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
				};
				
				temp.Show();
				d_templateParent.PackStart(box, false, false, 0);
				
				box.ButtonPressEvent += delegate(object o, ButtonPressEventArgs args) {
					TemplateActivated(this, template);
				};
			}
			
			if (templates.Length == 0)
			{
				Label lbl = new Label("<i>none</i>");
				lbl.UseMarkup = true;
				lbl.Show();
				
				d_templateParent.PackStart(lbl, false, false, 0);
			}
			
			Alignment align = new Alignment(0, 0, 1, 1);
			align.LeftPadding = 3;
			align.Show();
			
			Button but = new Button();
			but.Relief = ReliefStyle.None;

			Image img = new Image(Gtk.Stock.Add, IconSize.Menu);
			img.Show();
			
			RcStyle style = new RcStyle();
			style.Xthickness = 0;
			style.Ythickness = 0;
			
			but.ModifyStyle(style);

			but.Add(img);
			but.Show();
			
			align.Add(but);
			
			but.Clicked += AddTemplateClicked;
			
			d_templateParent.PackStart(align, false, false, 0);
		}
		
		private void AddTemplateClicked(object source, EventArgs args)
		{
			if (d_findTemplate == null)
			{
				Gtk.Window par = (Gtk.Window)Toplevel;

				d_findTemplate = new Dialogs.FindTemplate(d_network.TemplateGroup, delegate (Wrappers.Wrapper node) {
					return (node is Wrappers.Link) == (d_object is Wrappers.Link);
				}, par);
				
				d_findTemplate.Destroyed += delegate (object sr, EventArgs ar)
				{
					d_findTemplate = null;
				};
				
				d_findTemplate.Response += delegate(object o, ResponseArgs arr) {
					if (arr.ResponseId == ResponseType.Apply)
					{
						foreach (Wrappers.Wrapper wrapper in d_findTemplate.Selection)
						{
							try
							{
								d_actions.ApplyTemplate(wrapper, new Wrappers.Wrapper[] {d_object});
							}
							catch (Exception e)
							{
								Error(this, e);
								break;
							}
						}
					}
					
					d_findTemplate.Destroy();
				};
			}
			
			d_findTemplate.Show();
		}
		
		private bool ObjectIsNetwork
		{
			get
			{
				return d_object != null && d_object is Wrappers.Network;
			}
		}
		
		private void AddIdUI()
		{
			HBox hbox = new HBox(false, 6);
			hbox.Show();

			Label lbl = new Label("Id:");
			lbl.Show();
			
			hbox.PackStart(lbl, false, false, 0);
			
			d_entry = new Entry();
			d_entry.Show();
			
			d_entry.WidthChars = 15;
			
			d_entry.Text = d_object.Id;
			
			// This is a bit hacky
			d_entry.Sensitive = !ObjectIsNetwork;
			
			d_entry.Activated += delegate {
				ModifyId();
			};
			
			d_entry.FocusOutEvent += delegate {
				ModifyId();
			};
			
			d_entry.KeyPressEvent += delegate(object o, KeyPressEventArgs args) {
				if (args.Event.Key == Gdk.Key.Escape)
				{
					d_entry.Text = d_object.Id;
					d_entry.Position = d_entry.Text.Length;
				}
			};
			
			hbox.PackStart(d_entry, false, false, 0);
			
			HBox templateBox = new HBox(false, 0);
			
			lbl = new Label("« (");
			lbl.Show();

			templateBox.PackStart(lbl, false, false, 0);
			
			d_templateParent = new HBox(false, 0);
			
			RebuildTemplateWidgets();
			templateBox.PackStart(d_templateParent, false, false, 0);
			
			lbl = new Label(")");
			lbl.Show();
			templateBox.PackStart(lbl, false, false, 0);
			
			hbox.PackStart(templateBox, false, false, 0);
			d_extraControl.PackStart(hbox, false, false, 0);
		}

		private void HandleTemplateLabelStyleSet(object o, StyleSetArgs args)
		{
			Label lbl = o as Label;

			Gdk.Color linkColor = (Gdk.Color)lbl.StyleGetProperty("link-color");
			
			lbl.StyleSet -= HandleTemplateLabelStyleSet;
			lbl.ModifyFg(StateType.Normal, linkColor);
			lbl.ModifyFg(StateType.Prelight, linkColor);
			lbl.ModifyFg(StateType.Active, linkColor);
			lbl.ModifyFg(StateType.Insensitive, linkColor);
			lbl.StyleSet += HandleTemplateLabelStyleSet;
		}

		private void ModifyId()
		{ 
			if (d_object.Id == d_entry.Text || d_entry.Text == "")
			{
				d_entry.Text = d_object.Id;
				return;
			}
			
			d_actions.Do(new Undo.ModifyObjectId(d_object, d_entry.Text));
		}
		
		public void Initialize(Wrappers.Wrapper obj)
		{
			Initialize(obj, false);
		}
		
		private void Initialize(Wrappers.Wrapper obj, bool force)
		{
			if (!force && obj == d_object)
			{
				return;
			}

			Clear();
			
			InitializeFlagsList();
			
			d_object = obj;
			
			d_paned = new HPaned();

			d_paned.Realized += delegate {
				d_paned.Position = Allocation.Width / 2;
			};
			
			if (d_object != null && d_object is Wrappers.Link)
			{
				AddEquationsUI();
			}
			
			Gtk.VBox vbox = new Gtk.VBox(false, 3);
			d_paned.Add1(vbox);

			d_extraControl = new HBox(false, 12);
			d_extraControl.Show();
			PackStart(d_extraControl, false, false, 0);
			
			PackStart(d_paned, true, true, 0);
			
			if (d_object != null)
			{
				AddIdUI();
			}
			
			if (d_object != null && d_object is Wrappers.Group)
			{
				AddGroupUI();
			}
			
			Wrappers.InputFile input = null;
			
			if (d_object != null)
			{
				input = d_object as Wrappers.InputFile;
			}
			
			if (input != null)
			{
				AddInputFileUI();
			}

			ScrolledWindow vw = new ScrolledWindow();
			vw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			vw.ShadowType = ShadowType.EtchedIn;
			
			d_store = new NodeStore<PropertyNode>();
			d_treeview = new TreeView(new TreeModelAdapter(d_store));
			
			d_treeview.RulesHint = true;
			d_treeview.Selection.Mode = SelectionMode.Multiple;
			d_treeview.ButtonPressEvent += OnTreeViewButtonPressEvent;
			
			d_store.NodeChanged += HandleNodeChanged;
			
			d_treeview.ShowExpanders = false;
			
			vw.Add(d_treeview);
			
			d_treeview.Show();
			vw.Show();

			vbox.PackStart(vw, true, true, 0);
			
			CellRendererText renderer;
			TreeViewColumn column;
			
			// Add column for the name
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			column = new TreeViewColumn("Name", renderer, "text", 0);
			column.Resizable = true;
			column.MinWidth = 75;
			
			column.SetCellDataFunc(renderer, VisualizeProperties);
			
			if (d_object != null)
			{
				renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
					d_editingEntry = args.Editable as Entry;
					d_editingPath = args.Path;
				};

				renderer.EditingCanceled += delegate(object sender, EventArgs e) {
					if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
					{
						// Still do it actually
						NameEdited(d_editingEntry.Text, d_editingPath);
					}
				};

				renderer.Edited += DoNameEdited;
			}
			
			d_treeview.AppendColumn(column);
			
			// Add column for the value
			renderer = new CellRendererText();
			renderer.Editable = true;
			
			if (d_object != null)
			{
				renderer.EditingStarted += delegate(object o, EditingStartedArgs args) {
					d_editingEntry = args.Editable as Entry;
					d_editingPath = args.Path;
				};

				renderer.EditingCanceled += delegate(object sender, EventArgs e) {
					if (d_editingEntry != null && Utils.GetCurrentEvent() is Gdk.EventButton)
					{
						// Still do it actually
						ValueEdited(d_editingEntry.Text, d_editingPath);
					}
				};

				renderer.Edited += DoValueEdited;
			}
				
			column = new TreeViewColumn("Value", renderer, "text", 1);
			column.Resizable = true;

			if (input != null)
			{
				column.SetCellDataFunc(renderer, DisableInputProperties);
			}

			d_treeview.AppendColumn(column);
			
			// Add column for the integrated
			CellRendererToggle toggle = new CellRendererToggle();
			column = new TreeViewColumn("Integrated", toggle, "active", 2);
			column.Resizable = false;
			
			if (d_object != null)
			{
				toggle.Toggled += DoIntegratedToggled;
			}
			
			if (input != null)
			{
				column.SetCellDataFunc(toggle, DisableInputProperties);
			}

			d_treeview.AppendColumn(column);
				
			// Add column for property flags
			CellRendererCombo combo = new CellRendererCombo();
			combo.Editable = true;
			combo.Sensitive = true;
			
			column = new TreeViewColumn("Flags", combo, "text", 3);
			column.Resizable = true;
			
			combo.EditingStarted += DoEditingStarted;
			combo.Edited += DoFlagsEdited;
			combo.HasEntry = false;
			
			if (input != null)
			{
				column.SetCellDataFunc(combo, DisableInputProperties);
			}
			
			d_flagsStore = new ListStore(typeof(string));
			combo.Model = d_flagsStore;
			combo.TextColumn = 0;
			
			column.MinWidth = 50;
			d_treeview.AppendColumn(column);

			d_treeview.Selection.Changed += DoSelectionChanged;
			
			Connect();
			
			if (d_object != null)
			{
				InitStore();
				Sensitive = true;
			}
			else
			{
				Sensitive = false;
			}
			
			column = new TreeViewColumn();
			d_treeview.AppendColumn(column);
			
			ShowAll();
			
			d_propertyControls = new AddRemovePopup(d_treeview);
			d_propertyControls.AddButton.Clicked += DoAddProperty;
			d_propertyControls.RemoveButton.Clicked += DoRemoveProperty;
			
			UpdateSensitivity();
		}
		
		private void VisualizeProperties(TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			CellRendererText text = renderer as CellRendererText;

			if (d_object is Wrappers.Input)
			{
				DisableInputProperties(col, renderer, model, iter);
			}
			
			PropertyNode node = d_store.GetFromIter(iter);
			
			if (node.Property.Object.GetPropertyTemplate(node.Property, true) != null)
			{
				text.ForegroundGdk = d_treeview.Style.Foreground(Gtk.StateType.Insensitive);
			}
			else
			{
				text.ForegroundGdk = d_treeview.Style.Foreground(d_treeview.State);
			}
		}
		
		private void DisableInputProperties(TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			Wrappers.InputFile input = (Wrappers.InputFile)d_object;
			PropertyNode node = d_store.GetFromIter(iter);
			
			string[] columns = input.WrappedObject.Columns;
			bool iscol = Array.Exists(columns, item => item == node.Property.Name);
			
			CellRendererText text = renderer as CellRendererText;
			
			if (text != null)
			{
				text.Editable = !iscol;
				text.ForegroundGdk = iscol ? Style.Foreground(Gtk.StateType.Insensitive) : Style.Foreground(State);

				return;
			}
			
			CellRendererToggle toggle = renderer as CellRendererToggle;
			
			if (toggle != null)
			{
				toggle.Activatable = !iscol;
				return;
			}
			
			CellRendererCombo combo = renderer as CellRendererCombo;
			
			if (combo != null)
			{
				combo.Editable = !iscol;
				return;
			}
		}

		private void HandleNodeChanged(NodeStore<PropertyNode> store, Node child)
		{
			UpdateSensitivity();
		}
		
		private void FillFlagsStore(Cpg.Property property)
		{
			d_flagsStore.Clear();
			Cpg.PropertyFlags flags = property.Flags;
			Cpg.PropertyFlags building = Cpg.PropertyFlags.None;
			
			List<string> items = new List<string>();
			List<KeyValuePair<string, Cpg.PropertyFlags>> copy = new List<KeyValuePair<string, Cpg.PropertyFlags>>(d_flaglist);
			
			copy.Reverse();
			
			foreach (KeyValuePair<string, Cpg.PropertyFlags> pair in copy)
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

		private void DoEditingStarted(object o, EditingStartedArgs args)
		{
			FillFlagsStore(d_store.FindPath(args.Path).Property);
		}
		
		private void InitStore()
		{
			foreach (Cpg.Property prop in d_object.Properties)
			{
				AddProperty(prop);
			}
		}
		
		private void DoIntegratedToggled(object source, ToggledArgs args)
		{
			PropertyNode node = d_store.FindPath(args.Path);
			PropertyFlags flags = node.Property.Flags;
			CellRendererToggle toggle = (CellRendererToggle)source;
			
			if (!toggle.Active)
			{
				flags |= PropertyFlags.Integrated;
			}
			else
			{
				flags &= ~PropertyFlags.Integrated;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, node.Property, flags));
		}
		
		private void DoFlagsEdited(object source, EditedArgs args)
		{
			PropertyNode node = d_store.FindPath(args.Path);

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

			Cpg.PropertyFlags add_flags;
			Cpg.PropertyFlags remove_flags;
			
			Property.FlagsFromString(name, out add_flags, out remove_flags);
			Cpg.PropertyFlags newflags = node.Property.Flags;

			if (wason)
			{
				newflags &= ~add_flags;
			}
			else
			{
				newflags |= add_flags;
			}
			
			if (newflags == node.Property.Flags)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, node.Property, newflags));
		}
		
		private void ValueEdited(string newValue, string path)
		{
			PropertyNode node = d_store.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			if (newValue.Trim() == node.Property.Expression.AsString)
			{
				return;
			}
			
			d_actions.Do(new Undo.ModifyProperty(d_object, node.Property, newValue.Trim()));
		}
		
		private void DoValueEdited(object source, EditedArgs args)
		{
			ValueEdited(args.NewText, args.Path);
		}
		
		private void NameEdited(string newName, string path)
		{
			if (String.IsNullOrEmpty(newName))
			{
				return;
			}
			
			PropertyNode node = d_store.FindPath(path);
			
			if (node == null)
			{
				return;
			}
			
			if (newName.Trim() == node.Property.Name)
			{
				return;
			}

			List<Undo.IAction> actions = new List<Undo.IAction>();

			actions.Add(new Undo.RemoveProperty(d_object, node.Property));
			actions.Add(new Undo.AddProperty(d_object, newName.Trim(), node.Property.Expression.AsString, node.Property.Flags));
			
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
		
		enum Sensitivity
		{
			None,
			Revert,
			Remove
		}
		
		private void UpdateSensitivity()
		{
			Sensitivity sens = Sensitivity.None;
			
			if (d_treeview.Selection != null)
			{			
				foreach (TreePath path in d_treeview.Selection.GetSelectedRows())
				{
					PropertyNode node = d_store.FindPath(path);
					
					if (d_object.GetPropertyTemplate(node.Property, true) != null)
					{
						if (sens != Sensitivity.None)
						{
							sens = Sensitivity.None;
							break;
						}
					}
					else if (d_object.GetPropertyTemplate(node.Property, false) != null)
					{
						if (sens == Sensitivity.Remove)
						{
							sens = Sensitivity.None;
							break;
						}
						else
						{
							sens = Sensitivity.Revert;
						}
					}
					else
					{
						sens = Sensitivity.Remove;
					}
				}
			}
			
			d_propertyControls.RemoveButton.Sensitive = (sens != Sensitivity.None);

			if (sens == Sensitivity.Revert)
			{
				 d_propertyControls.RemoveButton.Image = new Image(Gtk.Stock.RevertToSaved, IconSize.Button);
			}
			else
			{
				d_propertyControls.RemoveButton.Image = new Image(Gtk.Stock.Remove, IconSize.Button);
			}
		}
		
		private void DoSelectionChanged(object source, EventArgs args)
		{
			UpdateSensitivity();			
		}
		
		private void UpdateActionSensitivity()
		{
			if (d_actionView.Selection.CountSelectedRows() == 0)
			{
				d_actionControls.RemoveButton.Sensitive = false;
			}
			else
			{
				d_actionControls.RemoveButton.Sensitive = true;
			}
		}
		
		private void UpdatePropertyInterfaceSensitivity()
		{
			if (d_interfacePropertyView.Selection.CountSelectedRows() == 0)
			{
				d_interfacePropertyControls.RemoveButton.Sensitive = false;
			}
			else
			{
				d_interfacePropertyControls.RemoveButton.Sensitive = true;
			}
		}
		
		private void DoActionSelectionChanged(object source, EventArgs args)
		{
			UpdateActionSensitivity();
		}
		
		private void HandleAddBinding()
		{
			DoAddProperty();
		}
		
		private void HandleDeleteBinding()
		{
			DoRemoveProperty();
		}
		
		private void AddProperty(Cpg.Property prop)
		{
			if (PropertyExists(prop.Name))
			{
				return;
			}
			
			TreeIter iter;
			d_store.Add(new PropertyNode(prop), out iter);
			
			if (d_selectProperty)
			{
				d_treeview.Selection.UnselectAll();
				d_treeview.Selection.SelectIter(iter);
				
				TreePath path = d_store.GetPath(iter);					
				d_treeview.SetCursor(path, d_treeview.GetColumn(0), true);
			}			
		}
		
		private void HandleIdChanged(object source, GLib.NotifyArgs args)
		{
			d_entry.Text = d_object.Id;
		}
		
		private bool PropertyExists(string name)
		{
			if (d_store.Find(name) != null)
			{
				return true;
			}
			
			Wrappers.Group grp = d_object as Wrappers.Group;
			
			if (grp != null)
			{
				if (grp.PropertyInterface.Lookup(name) != null)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private void DoAddProperty(object source, EventArgs args)
		{
			DoAddProperty();
		}
		
		private void DoAddProperty()
		{
			int num = 1;
			
			while (PropertyExists("x" + num))
			{
				++num;
			}
			
			d_selectProperty = true;
			d_actions.Do(new Undo.AddProperty(d_object, "x" + num, "0", Cpg.PropertyFlags.None));
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
				PropertyNode node = d_store.FindPath(path);
				
				Wrappers.Wrapper temp = d_object.GetPropertyTemplate(node.Property, false);
				
				if (temp != null)
				{
					Cpg.Property tempProp = temp.Property(node.Property.Name);

					actions.Add(new Undo.ModifyProperty(d_object, node.Property, tempProp.Expression.AsString));
					actions.Add(new Undo.ModifyProperty(d_object, node.Property, tempProp.Flags));
				}
				else
				{
					actions.Add(new Undo.RemoveProperty(d_object, node.Property));
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
		
		private void DoAddAction(object source, EventArgs args)
		{
			Wrappers.Link link = d_object as Wrappers.Link;
			
			List<string> props = new List<string>(Array.ConvertAll<LinkAction, string>(link.Actions, item => item.Target));
			List<string> prefs = new List<string>();
			
			if (link.To != null)
			{
				prefs = new List<string>(Array.ConvertAll<Property, string>(link.To.Properties, item => item.Name));
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
			d_actions.Do(new Undo.AddLinkAction(link, name, ""));
			d_selectAction = false;
		}
		
		private void DoRemoveAction(object source, EventArgs args)
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_actionView.Selection.GetSelectedRows())
			{
				LinkActionNode node = d_actionStore.FindPath(path);

				actions.Add(new Undo.RemoveLinkAction((Wrappers.Link)d_object, node.LinkAction));
			}
			
			d_actions.Do(new Undo.Group(actions));
		}
		
		private void DoPropertyAdded(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			AddProperty(prop);
		}
		
		private void DoPropertyRemoved(Wrappers.Wrapper obj, Cpg.Property prop)
		{
			d_store.Remove(prop);
		}
		
		private void UpdateInterfaceProperty(string name, string propid, string path)
		{
			InterfacePropertyNode node = d_interfacePropertyStore.FindPath(path);
			Wrappers.Group grp = d_object as Wrappers.Group;
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			if (node == null)
			{
				return;
			}
			
			if (name != null && name == node.Name)
			{
				return;
			}
			
			if (name != null && grp.PropertyInterface.Lookup(name) != null)
			{
				/* Already exists */
				Error(this, new Exception(String.Format("The interface `{0}' already exists on `{1}'", name, grp.FullId)));
				return;
			}
			
			if (propid != null && !propid.Contains("."))
			{
				Error(this, new Exception(String.Format("The interface target `{1}' does not refer to a child property", propid)));
				return;
			}
			
			/* Remove original */
			actions.Add(new Undo.RemoveInterfaceProperty(grp, node.Name, node.ChildName, node.PropertyName));
			
			if (name == null)
			{
				name = node.Name;
			}
			
			if (propid == null)
			{
				propid = node.Target;
			}
			
			string[] parts = propid.Split('.');
			
			actions.Add(new Undo.AddInterfaceProperty(grp, name, parts[0], parts[1]));
			
			try
			{
				d_actions.Do(new Undo.Group(actions));
			}
			catch (GLib.GException exception)
			{
				Error(this, exception);
			}
		}
		
		private void HandleInterfacePropertyNameEdited(object source, EditedArgs args)
		{
			/* Need to remove and re-add the interface */
			UpdateInterfaceProperty(args.NewText, null, args.Path);
		}
		
		private void HandleInterfacePropertyTargetEditingStarted(object source, EditingStartedArgs args)
		{
			Entry entry = args.Editable as Entry;
			
			if (entry == null)
			{
				return;
			}
			
			entry.PopulatePopup += delegate(object o, PopulatePopupArgs popargs) {
				MenuItem g = new MenuItem("Targets");
				Menu sub = new Menu();

				Wrappers.Group grp = d_object as Wrappers.Group;
				
				foreach (Wrappers.Object child in grp.Children)
				{
					MenuItem c = new MenuItem(child.Id.Replace("_", "__"));
					Menu m = new Menu();

					foreach (Cpg.Property prop in child.Properties)
					{
						MenuItem p = new MenuItem(prop.Name.Replace("_", "__"));
						m.Append(p);
						
						Wrappers.Object theChild = child;
						Cpg.Property theProperty = prop;
						
						p.Activated += delegate(object sender, EventArgs e) {
							entry.Text = theChild.Id + "." + theProperty.Name;
						};
					}
					
					c.Submenu = m;
					sub.Append(c);
				}
				
				g.Submenu = sub;
				g.ShowAll();
				
				SeparatorMenuItem sep = new SeparatorMenuItem();
				sep.Show();
								
				popargs.Menu.Append(sep);
				popargs.Menu.Append(g);
			};
		}

		private void HandleInterfacePropertyTargetEdited(object source, EditedArgs args)
		{
			/* Need to remove and re-add the interface */
			InterfacePropertyNode node = d_interfacePropertyStore.FindPath(args.Path);
			
			if (node != null && node.Target == args.NewText)
			{
				return;
			}

			UpdateInterfaceProperty(null, args.NewText, args.Path);
		}
		
		private void HandleGroupInterfacePropertyAdded(object source, Cpg.AddedArgs args)
		{
			AddGroupPropertyInterface(args.Name);
		}
		
		private void HandleGroupInterfacePropertyRemoved(object source, Cpg.RemovedArgs args)
		{
			d_interfacePropertyStore.Remove(args.Name);
		}
		
		private void DoAddInterfaceProperty(object source, EventArgs args)
		{
			Wrappers.Group grp = d_object as Wrappers.Group;
			Cpg.PropertyInterface iface = grp.WrappedObject.PropertyInterface;
			
			int i = 1;
			string name;
			
			while (true)
			{
				name = String.Format("x_{0}", i++);
				
				if (iface.Lookup(name) == null)
				{
					break;
				}
			}
			
			Cpg.Property prop = null;
			
			/* Select first property */
			foreach (Wrappers.Object child in grp.Children)
			{
				Cpg.Property[] props = child.Properties;
				
				if (props.Length != 0)
				{
					prop = props[0];
					break;
				}
			}
			
			if (prop == null)
			{
				return;
			}
			
			d_selectInterface = true;
			
			try
			{
				d_actions.Do(new Undo.AddInterfaceProperty(grp, name, prop.Object.Id, prop.Name));
			}
			catch (GLib.GException exception)
			{
				Error(this, exception);
			}

			d_selectInterface = false;
		}
		
		private void DoRemoveInterfaceProperty(object source, EventArgs args)
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();

			foreach (TreePath path in d_interfacePropertyView.Selection.GetSelectedRows())
			{
				InterfacePropertyNode node = d_interfacePropertyStore.FindPath(path);

				actions.Add(new Undo.RemoveInterfaceProperty((Wrappers.Group)d_object, node.Name, node.ChildName, node.PropertyName));
			}
			
			try
			{
				d_actions.Do(new Undo.Group(actions));
			}
			catch (GLib.GException exception)
			{
				Error(this, exception);
			}
		}
		
		private void AddGroupPropertyInterface(string name)
		{
			TreeIter iter;
			Wrappers.Group grp = d_object as Wrappers.Group;
			
			d_interfacePropertyStore.Add(new InterfacePropertyNode(grp.PropertyInterface, name), out iter);
			
			if (d_selectInterface)
			{
				d_interfacePropertyView.Selection.UnselectAll();
				d_interfacePropertyView.Selection.SelectIter(iter);
				
				TreePath path = d_interfacePropertyStore.GetPath(iter);					
				d_interfacePropertyView.SetCursor(path, d_interfacePropertyView.GetColumn(0), true);
			}
		}

		private void RemoveGroupPropertyInterface(string name)
		{
			d_interfacePropertyStore.Remove(name);
		}
		
		private void DoInterfacePropertySelectionChanged(object source, EventArgs args)
		{
			UpdatePropertyInterfaceSensitivity();
		}
		
		private void Clear()
		{
			Disconnect();

			while (Children.Length > 0)
			{
				Remove(Children[0]);
			}
			
			if (d_store != null)
			{
				d_store.Clear();
			}
				
			d_object = null;
			Sensitive = false;
		}
		
		public void Select(Cpg.Property property)
		{
			TreeIter iter;
			
			if (d_store.Find(property, out iter))
			{
				d_treeview.Selection.SelectIter(iter);
			}
		}
		
		public void Select(Cpg.LinkAction action)
		{
			TreeIter iter;
			
			if (d_actionStore.Find(action, out iter))
			{
				d_actionView.Selection.SelectIter(iter);
			}
		}
		
		protected override void OnRealized()
		{
			base.OnRealized();
			
			d_paned.Position = Allocation.Width / 2;
		}

		[GLib.ConnectBefore]
		private void OnTreeViewButtonPressEvent(object source, ButtonPressEventArgs args)
		{
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
	}
}
