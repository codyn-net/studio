using System;
namespace Cdn.Studio.Widgets
{
	public class ScrolledWindow : Gtk.ScrolledWindow
	{
		public ScrolledWindow()
		{
			Gtk.RcStyle rc = new Gtk.RcStyle();
			
			rc.Xthickness = 1;
			rc.Ythickness = 1;
			
			ModifyStyle(rc);
		}
	}
}

