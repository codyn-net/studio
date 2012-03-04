using System;
using Gtk;

namespace Cdn.Studio.Widgets
{
	public class Annotation : Gtk.Alignment
	{
		private VBox d_vbox;
		private Label d_labelInfo;
		private EventBox d_eventBox;
		private TextView d_editor;
		private ScrolledWindow d_editorWindow;
		private string d_info;
		private string d_title;
		private Cdn.Annotatable d_annotatable;
		
		public event EventHandler TitleChanged = delegate {};

		public Annotation() : base(0, 0, 1, 1)
		{
			d_vbox = new VBox(false, 6);
			d_vbox.Show();
			
			SetPadding(6, 6, 6, 6);

			d_eventBox = new EventBox();
			d_eventBox.Show();
			d_eventBox.AddEvents((int)Gdk.EventMask.AllEventsMask);

			d_labelInfo = new Label();
			d_labelInfo.Show();
			d_labelInfo.SetAlignment(0, 0);
			d_labelInfo.Wrap = true;
			d_labelInfo.LineWrapMode = Pango.WrapMode.WordChar;
			
			d_eventBox.Add(d_labelInfo);
			d_vbox.PackStart(d_eventBox, true, true, 0);
			
			d_editorWindow = new ScrolledWindow();
			d_editorWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			d_editorWindow.ShadowType = ShadowType.EtchedIn;
			
			d_editor = new TextView();
			d_editor.WrapMode = WrapMode.WordChar;
			d_editor.Show();

			d_editorWindow.Add(d_editor);

			d_vbox.PackStart(d_editorWindow, true, true, 0);
			
			d_eventBox.ButtonPressEvent += OnEventBoxButtonPress;
			
			d_editor.KeyPressEvent += OnEditorKeyPressEvent;
			d_editor.FocusOutEvent += OnEditorFocusOutEvent;
			
			Update(null);
			
			Add(d_vbox);
		}

		private void OnEditorFocusOutEvent(object o, FocusOutEventArgs args)
		{
			StopEdit(false);
		}

		private void OnEditorKeyPressEvent(object o, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Escape)
			{
				StopEdit(false);
				args.RetVal = true;
			}
		}

		private void OnEventBoxButtonPress(object o, ButtonPressEventArgs args)
		{
			if (args.Event.Type != Gdk.EventType.TwoButtonPress || args.Event.Button != 1)
			{
				return;
			}
			
			StartEdit();
		}
		
		private void StartEdit()
		{
			if (d_annotatable == null)
			{
				return;
			}
			
			d_editor.Buffer.Text = d_annotatable.Annotation != null ? d_annotatable.Annotation : "";
			
			d_eventBox.Hide();
			d_editorWindow.Show();
			
			d_editor.GrabFocus();
		}
		
		private void StopEdit(bool cancelled)
		{
			if (d_annotatable == null || !d_editorWindow.Visible)
			{
				return;
			}
			
			if (!cancelled)
			{
				string text = d_editor.Buffer.Text.Trim();
				
				if (text == "" && d_annotatable != null)
				{
					d_annotatable.Annotation = null;
				}

				Info = text != "" ? text : null;
			}
			
			d_editorWindow.Hide();
			d_eventBox.Show();
		}
		
		public string Title
		{
			get
			{
				return d_title;
			}
			set
			{
				d_title = value != null ? value : "<b>No selection</b>";
				
				TitleChanged(this, new EventArgs());
			}
		}
		
		public string Info
		{
			get
			{
				return d_info;
			}
			set
			{
				d_info = value != null ? value : "<i>Double-click to add information...</i>";
				d_labelInfo.Markup = d_info;
				
				if (d_annotatable != null && value != null)
				{
					d_annotatable.Annotation = d_info;
				}
			}
		}
		
		public void Update(Cdn.Annotatable annotatable)
		{
			StopEdit(false);

			d_annotatable = null;
			
			if (annotatable != null)
			{
				Title = annotatable.Title;
				Info = annotatable.Annotation;
			}
			else
			{
				Title = null;
				Info = null;
			}
			
			d_annotatable = annotatable;
		}
	}
}

