using System;
using System.Collections.Generic;

namespace Cpg.Studio
{
	public class Actions
	{
		private Undo.Manager d_undoManager;

		public Actions(Undo.Manager undoManager)
		{
			d_undoManager = undoManager;
		}
		
		public void AddState(Wrappers.Group parent, int x, int y)
		{
			Wrappers.State state = new Wrappers.State();
			state.Allocation = new Allocation(x, y, 1, 1);
			
			Do(new Undo.AddObject(parent, state));
		}
		
		private IEnumerable<KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>> GetLinkPairs(Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);

			sel.RemoveAll(item => item is Wrappers.Link);
			
			if (sel.Count > 0)
			{
				Wrappers.Wrapper last = sel[sel.Count - 1];
				
				if (sel.Count == 1)
				{
					// Self link
					yield return new KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>(last, last);
				}
			
				for (int i = 0; i < sel.Count - 1; ++i)
				{
					yield return new KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>(sel[i], last);
				}
			}
		}
		
		public void AddLink(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			// Add links between each first selected N-1 objects and selected object N
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper> pair in GetLinkPairs(selection))
			{
				Wrappers.Link link = new Wrappers.Link(new Cpg.Link("link", pair.Key, pair.Value));
				actions.Add(new Undo.AddObject(parent, link));
			}

			Do(new Undo.Group(actions));
		}
		
		private bool OnlyLinks(List<Wrappers.Wrapper> wrappers)
		{
			return !wrappers.Exists(item => !(item is Wrappers.Link));
		}
		
		private List<Wrappers.Wrapper> NormalizeSelection(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);
			
			if (parent != null)
			{
				foreach (Wrappers.Wrapper child in parent.Children)
				{
					if (sel.Contains(child) || !(child is Wrappers.Link))
					{
						continue;
					}
					
					Wrappers.Link link = (Wrappers.Link)child;
				
					if (sel.Contains(link.To) || sel.Contains(link.From))
					{
						sel.Add(link);
					}
				}
			}
			else
			{
				if (OnlyLinks(sel))
				{
					// Only links, that is fine and special!
					return sel;
				}

				sel.RemoveAll(delegate (Wrappers.Wrapper wrapper) {
					Wrappers.Link link = wrapper as Wrappers.Link;
				
					return link != null && (sel.Contains(link.To) && sel.Contains(link.From));
				});
			}
			
			return sel;
		}
		
		private List<Wrappers.Wrapper> NormalizeSelection(Wrappers.Wrapper[] selection)
		{
			return NormalizeSelection(null, selection);
		}
		
		public void Delete(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = NormalizeSelection(parent, selection);
			
			if (sel.Count == 0)
			{
				return;
			}
			
			// Remove them all!
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper child in sel)
			{
				actions.Add(new Undo.RemoveObject(child));
			}
			
			Do(new Undo.Group(actions));
		}
		
		public Wrappers.Group Group(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);

			if (OnlyLinks(sel))
			{
				return null;
			}

			// Collect all the links that go from or to the group, but are not fully in there
			Wrappers.Wrapper proxy = null;

			List<Wrappers.Link> proxyLinks = new List<Wrappers.Link>();

			foreach (Wrappers.Link link in Utils.FilterLink(parent.Children))
			{
				bool containsTo = sel.Contains(link.To);
				bool containsFrom = sel.Contains(link.From);

				if (containsTo != containsFrom)
				{
					if (proxy == null)
					{
						proxy = containsTo ? link.To : link.From;
					}
					else if ((containsTo && link.To != proxy) || (containsFrom && link.From != proxy))
					{
						throw new Exception(String.Format("Links outside the group are acting on different objects in the group. The current behavior of the network cannot be preserved."));
					}
					
					proxyLinks.Add(link);
				}
			}
			
			if (proxy != null && parent.Proxy != null && parent.Proxy != proxy)
			{
				throw new Exception(String.Format("Links outside the group are acting on an object which is different from the proxy of the current parent. The current behavior of the network cannot be preserved."));
			}
			else if (proxy == null)
			{
				proxy = parent.Proxy;
			}
			
			// Collect all objects and link fully encapsulated in the group
			List<Wrappers.Wrapper> ingroup = new List<Wrappers.Wrapper>();
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper wrapper in sel)
			{
				Wrappers.Link link = wrapper as Wrappers.Link;
				
				if (link == null || (sel.Contains(link.To) && sel.Contains(link.From)))
				{
					ingroup.Add(wrapper);
				}
				
				if (proxy == null && !(wrapper is Wrappers.Link))
				{
					proxy = wrapper;
				}
				
				// Also fill the first actions that remove all the objects from the parent
				actions.Add(new Undo.RemoveObject(parent, wrapper));
			}
		
			// After objects are removed, we create a new group
			double x;
			double y;

			Utils.MeanPosition(ingroup, out x, out y);

			Wrappers.Group newGroup = new Wrappers.Group();
			newGroup.Allocation.X = (int)x;
			newGroup.Allocation.Y = (int)y;
			
			actions.Add(new Undo.AddObject(parent, newGroup));
			
			if (proxy == parent.Proxy)
			{
				actions.Add(new Undo.ModifyProxy(parent, newGroup));
			}
			
			// Then we add all the 'ingroup' objects to the group
			foreach (Wrappers.Wrapper wrapper in ingroup)
			{
				// Move object to center at 0, 0 in the group
				if (!(wrapper is Wrappers.Link))
				{
					actions.Add(new Undo.MoveObject(wrapper, -(int)x, -(int)y));
				}
				
				// Add object to the group
				actions.Add(new Undo.AddObject(newGroup, wrapper));
			}
			
			// Set the proxy if needed
			if (proxy != null)
			{
				actions.Add(new Undo.ModifyProxy(newGroup, proxy));
			}
			
			// Then reconnect all the proxy links to the group instead
			foreach (Wrappers.Link link in proxyLinks)
			{
				if (sel.Contains(link.From))
				{
					actions.Add(new Undo.AttachLink(link, newGroup, link.To));
				}
				else
				{
					actions.Add(new Undo.AttachLink(link, link.From, newGroup));
				}
			}
			
			Do(new Undo.AddGroup(newGroup, actions));
			return newGroup;
		}
		
		public void Ungroup(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper wrapper in selection)
			{
				Wrappers.Group grp = wrapper as Wrappers.Group;
				
				if (grp != null)
				{
					actions.AddRange(Ungroup(parent, grp));
				}
			}
			
			if (actions.Count == 0)
			{
				return;
			}
			
			Do(new Undo.Ungroup(parent, actions.ToArray()));
		}
		
		private bool HasLinks(Wrappers.Wrapper grp)
		{
			if (grp.Links.Count != 0)
			{
				return true;
			}
			
			foreach (Wrappers.Link link in Utils.FilterLink(grp.Parent.Children))
			{
				if (link.From == grp)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private Undo.IAction[] Ungroup(Wrappers.Group parent, Wrappers.Group grp)
		{
			// Check if links can be redirected to the proxy
			if (HasLinks(grp) && grp.Proxy == null)
			{
				throw new Exception(String.Format("The group `{0}' has links but no proxy to redirect the links to when ungrouping", grp.Id));
			}
			
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			// Unset the proxy so that the undo also sets the proxy again
			if (grp.Proxy != null)
			{
				actions.Add(new Undo.ModifyProxy(grp, null));
			}
			
			// Remove all objects from the group
			foreach (Wrappers.Wrapper wrapper in grp.Children)
			{
				actions.Add(new Undo.RemoveObject(grp, wrapper));
			}
			
			// Remove the group itself
			actions.Add(new Undo.RemoveObject(parent, grp));
			
			double cx;
			double cy;

			Utils.MeanPosition(grp.Children, out cx, out cy);
			
			int dx = (int)(grp.Allocation.X - cx);
			int dy = (int)(grp.Allocation.Y - cy);
			
			foreach (Wrappers.Wrapper wrapper in grp.Children)
			{
				if (!(wrapper is Wrappers.Link))
				{
					// Center the objects around the position of the group
					actions.Add(new Undo.MoveObject(wrapper, dx, dy));
				}
				
				// Add object to the parent
				actions.Add(new Undo.AddObject(parent, wrapper));
			}
			
			// Reattach any links coming from or going to the group, redirect to proxy
			foreach (Wrappers.Link link in Utils.FilterLink(grp.Parent.Children))
			{
				if (link.To == grp && link.From == grp)
				{
					actions.Add(new Undo.AttachLink(link, grp.Proxy, grp.Proxy));
				}
				else if (link.To == grp)
				{
					actions.Add(new Undo.AttachLink(link, link.From, grp.Proxy));
				}
				else if (link.From == grp)
				{
					actions.Add(new Undo.AttachLink(link, grp.Proxy, link.To));
				}
			}
			
			// Copy properties defined on the group to the proxy
			if (grp.Proxy != null)
			{
				foreach (Cpg.Property prop in grp.Properties)
				{
					if (!grp.PropertyIsProxy(prop.Name))
					{
						actions.Add(new Undo.AddProperty(grp.Proxy, prop));
					}
				}
			}
			
			return actions.ToArray();
		}
		
		private Wrappers.Wrapper[] MakeCopy(Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = NormalizeSelection(selection);
			
			if (sel.Count == 0)
			{
				return new Wrappers.Wrapper[] {};
			}
			
			Dictionary<Cpg.Object, Wrappers.Wrapper> map = new Dictionary<Cpg.Object, Wrappers.Wrapper>();
			List<Wrappers.Wrapper> copied = new List<Wrappers.Wrapper>();
			
			// Create copies and store in a map the mapping from the orig to the copy
			foreach (Wrappers.Wrapper wrapper in sel)
			{
				Wrappers.Wrapper copy = wrapper.Copy();
				
				map[wrapper] = copy;
				copied.Add(copy);
			}
			
			// Reconnect links
			foreach (Wrappers.Link link in Utils.FilterLink(sel))
			{
				if (map.ContainsKey(link.From) && map.ContainsKey(link.To))
				{
					Wrappers.Wrapper from = map[link.From];
					Wrappers.Wrapper to = map[link.To];
				
					Wrappers.Link target = (Wrappers.Link)map[link.WrappedObject];
					target.Attach(from, to);
				}
			}
			
			return copied.ToArray();
		}
		
		public void Copy(Wrappers.Wrapper[] selection)
		{
			Wrappers.Wrapper[] sel = MakeCopy(selection);
			
			if (sel.Length == 0)
			{
				return;
			}
			
			Clipboard.Internal.Objects = sel;
			
			// TODO: serialize to XML too
		}
		
		public void Cut(Wrappers.Group parent, Wrappers.Wrapper[] selection)
		{
			Copy(selection);
			Delete(parent, selection);
		}
		
		public void Paste(Wrappers.Group parent, Wrappers.Wrapper[] selection, int dx, int dy)
		{
			if (Clipboard.Internal.Empty)
			{
				return;
			}
			
			// See if this is a special link only paste
			List<Wrappers.Wrapper> clip = new List<Wrappers.Wrapper>(Clipboard.Internal.Objects);
			List<Undo.IAction> actions = new List<Undo.IAction>();

			if (OnlyLinks(clip))
			{
				// Add links between each first selected N-1 objects and selected object N
				List<KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>> pairs = new List<KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>>(GetLinkPairs(selection));
			
				foreach (Wrappers.Wrapper obj in clip)
				{
					Wrappers.Link link = (Wrappers.Link)obj;
					
					foreach (KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper> pair in pairs)
					{
						Wrappers.Link copy = (Wrappers.Link)link.Copy();
						copy.Attach(pair.Key, pair.Value);

						actions.Add(new Undo.AddObject(parent, copy));
					}
				}
			}
			else
			{
				// Paste the new objects by making a copy (yes, again)
				Wrappers.Wrapper[] copied = MakeCopy(Clipboard.Internal.Objects);
			
				double x;
				double y;
	
				Utils.MeanPosition(copied, out x, out y);
				
				dx -= (int)x;
				dy -= (int)y;
				
				foreach (Wrappers.Wrapper wrapper in copied)
				{
					wrapper.Allocation.X += dx;
					wrapper.Allocation.Y += dy;
					
					actions.Add(new Undo.AddObject(parent, wrapper));
				}
			}
			
			Do(new Undo.Group(actions));
		}
		
		public void Move(List<Wrappers.Wrapper> all, int dx, int dy)
		{
			List<Wrappers.Wrapper> objs = new List<Wrappers.Wrapper>(all);
			objs.RemoveAll(item => item is Wrappers.Link);
			
			if (objs.Count == 0)
			{
				return;
			}
			
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper obj in objs)
			{
				actions.Add(new Undo.MoveObject(obj, dx, dy));
			}
			
			Do(new Undo.Group(actions));
		}
		
		public void Do(Undo.IAction action)
		{
			d_undoManager.Do(action);
		}
	}
}

