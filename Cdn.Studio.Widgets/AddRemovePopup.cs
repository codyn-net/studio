using System;

namespace Cpg.Studio.Widgets
{
	public class AddRemovePopup : Gtk.Window
	{
		private Gtk.Button d_removeButton;
		private Gtk.Button d_addButton;
		private Gtk.TreeView d_treeview;
		private uint d_leaveTimeout;

		public AddRemovePopup(Gtk.TreeView treeview) : base(Gtk.WindowType.Popup)
		{
			d_treeview = treeview;
			
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			TypeHint = Gdk.WindowTypeHint.Utility;
			
			BuildUI();

			d_treeview.EnterNotifyEvent += HandleEnterNotifyEvent;
			d_treeview.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			d_treeview.Destroyed += delegate {
				Destroy();
			};
		}
		
		protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
		{
			if (d_leaveTimeout != 0)
			{
				GLib.Source.Remove(d_leaveTimeout);
				d_leaveTimeout = 0;
			}

			return base.OnEnterNotifyEvent(evnt);
		}

		private void HandleLeaveNotifyEvent(object o, Gtk.LeaveNotifyEventArgs args)
		{
			if (d_leaveTimeout != 0)
			{
				GLib.Source.Remove(d_leaveTimeout);
			}

			d_leaveTimeout = GLib.Timeout.Add(100, LeaveTimeout);
			args.RetVal = false;
		}
		
		private bool LeaveTimeout()
		{
			d_leaveTimeout = 0;
			
			Hide();
			return false;
		}

		private void HandleEnterNotifyEvent(object o, Gtk.EnterNotifyEventArgs args)
		{
			if (d_leaveTimeout != 0)
			{
				GLib.Source.Remove(d_leaveTimeout);
				d_leaveTimeout = 0;
			}

			Move();
			Show();
			
			args.RetVal = false;
		}
		
		private void Move()
		{
			Gdk.Window treewindow = d_treeview.GdkWindow;
			
			Realize();
			
			int treeX;
			int treeY;
			int treeWidth;
			int treeHeight;

			treewindow.GetOrigin(out treeX, out treeY);
			treewindow.GetSize(out treeWidth, out treeHeight);

			int x = treeX + treeWidth - Allocation.Width - 6;
			int y = treeY + treeHeight - Allocation.Height - 6;
			
			Move(x, y);
		}
		
		private void BuildUI()
		{
			Gtk.HBox hbox = new Gtk.HBox(false, 3);
			Add(hbox);
			
			d_removeButton = new Gtk.Button();
			d_removeButton.Add(new Gtk.Image(Gtk.Stock.Remove, Gtk.IconSize.Menu));
			hbox.PackStart(d_removeButton, false, false ,0);

			d_addButton = new Gtk.Button();
			d_addButton.Add(new Gtk.Image(Gtk.Stock.Add, Gtk.IconSize.Menu));
			hbox.PackStart(d_addButton, false, false, 0);
			
			hbox.ShowAll();
		}
		
		public Gtk.Button AddButton
		{
			get
			{
				return d_addButton;
			}
		}
		
		public Gtk.Button RemoveButton
		{
			get
			{
				return d_removeButton;
			}
		}
	}
}

