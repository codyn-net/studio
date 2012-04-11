using System;
using Gtk;
using System.Collections.Generic;

namespace Cdn.Studio.Widgets.Editors
{
	public class Node : Gtk.HBox
	{
		private Wrappers.Node d_group;
		private ListStore d_proxyStore;
		private ComboBox d_proxyCombo;
		private Actions d_actions;

		public Node(Wrappers.Node grp, Actions actions) : base(false, 6)
		{
			d_group = grp;
			d_actions = actions;
			
			Build();
			
			Sensitive = (d_group != null);
		}
		
		private bool ObjectIsNetwork
		{
			get
			{
				return d_group != null && d_group is Wrappers.Network;
			}
		}

		private void Build()
		{
			Label label = new Label("Proxy:");
			label.Show();
			
			PackStart(label, false, false, 0);
			
			ListStore store = new ListStore(typeof(string), typeof(Wrappers.Wrapper), typeof(bool));

			List<Wrappers.Wrapper> children = new List<Wrappers.Wrapper>(d_group.Children);
			
			children.RemoveAll(item => item is Wrappers.Edge);

			children.Sort(delegate (Wrappers.Wrapper a, Wrappers.Wrapper b) {
				return a.Id.CompareTo(b.Id);
			});
			
			ComboBox box = new ComboBox(store);
			TreeIter iter;
			
			iter = store.AppendValues("None", null, false);
			
			if (d_group.Proxy == null)
			{
				box.SetActiveIter(iter);	
			}

			store.AppendValues(null, null, true);
			
			foreach (Wrappers.Wrapper child in children)
			{
				iter = store.AppendValues(child.Id, child, false);
				
				if (child == d_group.Proxy)
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
			
			PackStart(box, false, false, 0);
			
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
			
			do
			{
				Wrappers.Wrapper proxy = (Wrappers.Wrapper)d_proxyStore.GetValue(iter, 1);
				
				if (proxy == d_group.Proxy)
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
			
			d_actions.Do(new Undo.ModifyProxy(d_group, proxy));
		}
		
		private void Disconnect()
		{
			if (d_group == null)
			{
				return;
			}
			
			d_group.WrappedObject.RemoveNotification("proxy", HandleProxyChanged);
		}
		
		private void Connect()
		{
			if (d_group == null)
			{
				return;
			}
			
			d_group.WrappedObject.AddNotification("proxy", HandleProxyChanged);
		}
		
		protected override void OnDestroyed()
		{
			Disconnect();
		}
	}
}

