using System;
using Gtk;

namespace Cpg.Studio.Widgets.Editors
{
	public class Object : Gtk.HBox
	{
		public delegate void TemplateHandler(object source, Wrappers.Wrapper template);
		public event TemplateHandler TemplateActivated = delegate {};

		public delegate void ErrorHandler(object source, Exception exception);
		public event ErrorHandler Error = delegate {};

		private Wrappers.Wrapper d_object;
		private Entry d_entry;
		private HBox d_templateParent;
		private Dialogs.FindTemplate d_findTemplate;
		private Actions d_actions;
		private Wrappers.Network d_network;
		
		public Object(Wrappers.Wrapper obj, Actions actions, Wrappers.Network network) : base(false, 6)
		{
			d_actions = actions;
			d_object = obj;
			d_network = network;
			
			Build();
			
			Sensitive = (d_object != null);
			Connect();
		}
		
		private bool ObjectIsNetwork
		{
			get
			{
				return d_object != null && d_object is Wrappers.Network;
			}
		}
		
		public Wrappers.Wrapper WrappedObject
		{
			get
			{
				return d_object;
			}
		}
		
		private void Build()
		{
			Label lbl = new Label("Name:");
			lbl.Show();
			
			PackStart(lbl, false, false, 0);
			
			d_entry = new Entry();
			d_entry.Show();
			
			d_entry.WidthChars = 15;
			
			if (d_object != null)
			{
				d_entry.Text = d_object.Id;
			
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
			}
			
			PackStart(d_entry, false, false, 0);
			
			if (d_object != null && !(d_object is Wrappers.Function))
			{			
				HBox templateBox = new HBox(false, 0);
				templateBox.Show();
				
				lbl = new Label("« (");
				lbl.Show();
	
				templateBox.PackStart(lbl, false, false, 0);
				
				d_templateParent = new HBox(false, 0);
				d_templateParent.Show();
				
				RebuildTemplateWidgets();
	
				templateBox.PackStart(d_templateParent, false, false, 0);
				
				lbl = new Label(")");
				lbl.Show();
				templateBox.PackStart(lbl, false, false, 0);
				
				PackStart(templateBox, false, false, 0);
			}
		}
		
		private void RebuildTemplateWidgets()
		{
			if (d_object == null)
			{
				return;
			}

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

		private void HandleTemplateChanged(Wrappers.Wrapper source, Wrappers.Wrapper template)
		{
			RebuildTemplateWidgets();
		}
		
		private void HandleIdChanged(object source, GLib.NotifyArgs args)
		{
			d_entry.Text = d_object.Id;
		}

		private void Disconnect()
		{
			if (d_object == null)
			{
				return;
			}
			
			d_object.WrappedObject.RemoveNotification("id", HandleIdChanged);
			
			d_object.TemplateApplied -= HandleTemplateChanged;
			d_object.TemplateUnapplied -= HandleTemplateChanged;			
		}
		
		private void Connect()
		{
			if (d_object == null)
			{
				return;
			}
			
			d_object.WrappedObject.AddNotification("id", HandleIdChanged);
			
			d_object.TemplateApplied += HandleTemplateChanged;
			d_object.TemplateUnapplied += HandleTemplateChanged;			
		}
		
		protected override void OnDestroyed()
		{
			Disconnect();

			base.OnDestroyed();
		}
	}
}

