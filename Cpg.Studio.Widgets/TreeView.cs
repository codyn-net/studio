using System;
using System.Reflection;

namespace Cpg.Studio.Widgets
{
	public class TreeView : Gtk.TreeView
	{
		public TreeView() : base()
		{
		}
		
		public TreeView(Gtk.TreeModel model) : base(model)
		{
		}

		protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
		{
			base.OnLeaveNotifyEvent(evnt);
			return false;
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			base.OnEnterNotifyEvent(evnt);
			return false;
		}
	}
}

