using System;

namespace Cpg.Studio.Widgets
{
	public class Notebook : Gtk.Notebook
	{
		public Notebook()
		{
			Gtk.RcStyle rc = new Gtk.RcStyle();
			
			rc.Xthickness = 0;
			rc.Ythickness = 0;
			
			ModifyStyle(rc);
		}
	}
}

