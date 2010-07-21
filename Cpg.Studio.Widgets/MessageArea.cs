using System;
using Gtk;

namespace Cpg.Studio.Widgets
{
	public class MessageArea : HBox
	{
		private HBox d_mainBox;
		private VBox d_actionArea;
		private bool d_changingStyle;
		private Widget d_contents;
		private Label d_secondary;
		
		public delegate void ResponseHandler(object source, ResponseType type);
		public event ResponseHandler Response = delegate {};
		
		public MessageArea(params object[] actions)
		{
			d_mainBox = new HBox(false, 16);
			d_mainBox.BorderWidth = 8;
			d_mainBox.Show();
			
			d_actionArea = new VBox(false, 10);
			d_actionArea.Show();
			
			d_mainBox.PackEnd(d_actionArea, false, true, 0);
			
			PackStart(d_mainBox, true, true, 0);
			
			AppPaintable = true;
			CanFocus = true;
			
			d_mainBox.StyleSet += OnStyleSet;
			
			for (int i = 0; i < actions.Length; ++i)
			{
				if (i + 1 < actions.Length)
				{
					AddAction(actions[i] as string, (ResponseType)actions[i + 1]);
				}
			}
		}
		
		public static MessageArea Create(string icon, string primary, string secondary, params object[] actions)
		{
			MessageArea ret = new MessageArea(actions);
			
			Widget content;
			VBox vbox = new VBox(false, 6);
			
			if (icon != null)
			{
				HBox hbox = new HBox(false, 12);
				hbox.PackStart(new Image(icon, IconSize.Dialog), false, false, 0);
				hbox.PackStart(vbox, true, true, 0);
				
				content = hbox;
			}
			else
			{
				content = vbox;
			}	
			
			Label prim = new Label("<b>" + System.Security.SecurityElement.Escape(primary) + "</b>");
			prim.UseMarkup = true;
			prim.Xalign = 0;
			prim.UseUnderline = false;
			
			vbox.PackStart(prim, false, true, 0);
			
			Label sec = new Label(secondary);
			sec.Xalign = 0;
			sec.UseUnderline = false;
			sec.Wrap = true;
			
			ret.d_secondary = sec;
			
			vbox.PackStart(sec, false, true, 0);
			
			content.ShowAll();
			
			ret.Contents = content;
			
			return ret;
		}
		
		public static MessageArea Create(string primary, string secondary, params object[] actions)
		{
			return Create(null, primary, secondary, actions);
		}

		public Button AddAction(string stock, ResponseType resp)
		{
			Button button = new Button(stock);
			button.CanDefault = true;
			button.Show();
					
			if (resp == ResponseType.Help)
			{
				d_actionArea.PackEnd(button, false, false, 0);
			}
			else
			{
				d_actionArea.PackStart(button, false, false, 0);
			}
			
			button.Data["MessageAreaResponse"] = resp;
			
			button.Clicked += delegate (object source, EventArgs args)
			{
				EmitResponse(resp);
			};
			
			return button;
		}
		
		public void SetDefaultResponse(ResponseType resp)
		{
			foreach (Widget w in d_actionArea.Children)
			{
				if (!w.Data.ContainsKey("MessageAreaResponse"))
					continue;
					
				if ((ResponseType)w.Data["MessageAreaResponse"] == resp)
				{
					w.GrabDefault();
				}
			}
		}
		
		public void SetResponseSensitive(ResponseType resp, bool sensitive)
		{
			foreach (Widget w in d_actionArea.Children)
			{
				if (!w.Data.ContainsKey("MessageAreaResponse"))
					continue;
					
				if ((ResponseType)w.Data["MessageAreaResponse"] == resp)
				{
					w.Sensitive = false;
				}
			}
		}
		
		public VBox ActionArea
		{
			get
			{
				return d_actionArea;
			}
		}
		
		public Widget Contents
		{
			get
			{
				return d_contents;
			}
			set
			{
				if (d_contents != null)
				{
					d_mainBox.Remove(d_contents);
				}
				
				d_contents = value;
				d_mainBox.PackStart(d_contents, true, true, 0);
			}
		}

		void OnStyleSet(object o, StyleSetArgs args)
		{
			if (d_changingStyle)
			{
				return;
			}
			
			Gtk.Window wnd = new Gtk.Window(WindowType.Popup);
			wnd.Name = "gtk-tooltip";
			wnd.EnsureStyle();
			
			Gtk.Widget thisone = (Gtk.Widget)o;
			
			Style style = wnd.Style;
			d_changingStyle = true;
			thisone.Style = style;
			d_changingStyle = false;
			
			QueueDraw();
		}
		
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (d_secondary != null)
			{
				d_secondary.SetSizeRequest((int)(allocation.Width * 0.6), -1);
			}
		}

		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			Gtk.Style.PaintFlatBox(Style,
			                       evnt.Window,
			                       StateType.Normal,
			                       ShadowType.Out,
			                       evnt.Area,
			                       this,
			                       "tooltip",
			                       Allocation.X + 1,
			                       Allocation.Y + 1,
			                       Allocation.Width - 2,
			                       Allocation.Height - 2);
			
			return base.OnExposeEvent(evnt);
		}
		
		private void EmitResponse(ResponseType resp)
		{
			Response(this, resp);
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape)
			{
				EmitResponse(ResponseType.DeleteEvent);
				return true;
			}
			else
			{			
				return base.OnKeyPressEvent (evnt);
			}
		}

	}
}
