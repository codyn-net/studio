using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Cdn.Studio
{
	public class Actions
	{
		private Undo.Manager d_undoManager;

		public Actions(Undo.Manager undoManager)
		{
			d_undoManager = undoManager;
		}
		
		public Wrappers.Object[] AddFunction(Wrappers.Node parent, double x, double y)
		{
			Wrappers.Function func = new Wrappers.Function();
			func.Allocation = new Allocation(x, y, 1, 1);
			
			Do(new Undo.AddObject(parent, func));
			
			return new Wrappers.Object[] {func};
		}
		
		public Wrappers.Object[] AddPiecewisePolynomial(Wrappers.Node parent, double x, double y)
		{
			Wrappers.FunctionPolynomial func = new Wrappers.FunctionPolynomial();
			func.Allocation = new Allocation(x, y, 1, 1);
			
			Do(new Undo.AddObject(parent, func));
			
			return new Wrappers.Object[] {func};
		}
		
		public Wrappers.Object[] AddNode(Wrappers.Node parent, double x, double y)
		{
			Wrappers.Node node = new Wrappers.Node();
			node.Allocation = new Allocation(x, y, 1, 1);
			
			Do(new Undo.AddObject(parent, node));
			
			return new Wrappers.Object[] {node};
		}
		
		public void AddObject(Wrappers.Node parent, Wrappers.Wrapper wrapper)
		{
			AddObject(parent, wrapper, wrapper.Allocation.X, wrapper.Allocation.Y);
		}
		
		public void AddObject(Wrappers.Node parent, Wrappers.Wrapper wrapper, double x, double y)
		{
			wrapper.Allocation.X = x;
			wrapper.Allocation.Y = y;

			Do(new Undo.AddObject(parent, wrapper));
		}
		
		private IEnumerable<KeyValuePair<Wrappers.Node, Wrappers.Node>> GetLinkPairs(Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);

			sel.RemoveAll(item => !(item is Wrappers.Node));
			
			if (sel.Count == 0)
			{
				yield return new KeyValuePair<Wrappers.Node, Wrappers.Node>(null, null);
			}
			else if (sel.Count == 1)
			{
				// Self link
				yield return new KeyValuePair<Wrappers.Node, Wrappers.Node>(sel[0] as Wrappers.Node, sel[0] as Wrappers.Node);
			}
			else
			{			
				// Separate source and target selection
				List<Wrappers.Node> source = new List<Wrappers.Node>();
				List<Wrappers.Node> target = new List<Wrappers.Node>();
				
				foreach (Wrappers.Wrapper wrapper in sel)
				{
					if (wrapper.SelectedAlt)
					{
						target.Add(wrapper as Wrappers.Node);
					}
					else
					{
						source.Add(wrapper as Wrappers.Node);
					}
				}
				
				// If no special target selection was made, we do full coupling
				if (target.Count == 0)
				{
					target = source;
				}
				
				for (int i = 0; i < source.Count; ++i)
				{
					for (int j = 0; j < target.Count; ++j)
					{
						// Don't do self coupling though
						if (source[i] != target[j])
						{
							yield return new KeyValuePair<Wrappers.Node, Wrappers.Node>(source[i], target[j]);
						}
					}
				}
			}
		}
		
		public Wrappers.Edge[] AddEdge(Wrappers.Node parent, Wrappers.Wrapper[] selection, double cx, double cy)
		{
			return AddEdge(parent, null, selection, cx, cy);
		}
		
		public Wrappers.Edge[] AddEdge(Wrappers.Node parent, Wrappers.Edge temp, Wrappers.Wrapper[] selection, double cx, double cy)
		{
			// Add links from source to target selection. If there is no target selection, use source selection also as target selection
			List<Undo.IAction> actions = new List<Undo.IAction>();
			List<Wrappers.Edge> ret = new List<Wrappers.Edge>();
			
			foreach (KeyValuePair<Wrappers.Node, Wrappers.Node> pair in GetLinkPairs(selection))
			{
				Wrappers.Edge link;
				
				string name;
				
				if (pair.Key != null && pair.Value != null)
				{
					name = String.Format("{0}_to_{1}", pair.Key.Id, pair.Value.Id);
				}
				else
				{
					name = "link";
				}

				if (temp == null)
				{
					link = (Wrappers.Edge)Wrappers.Wrapper.Wrap(new Cdn.Edge(name, pair.Key, pair.Value));
				}
				else
				{
					link = (Wrappers.Edge)temp.CopyAsTemplate();
					link.Id = name;
				 
					link.Attach(pair.Key, pair.Value);
				}
				
				if (link.Empty)
				{
					link.Allocation.X = cx;
					link.Allocation.Y = cy;
				}
				
				ret.Add(link);
				actions.Add(new Undo.AddObject(parent, link));
			}

			Do(new Undo.Group(actions));
			return ret.ToArray();
		}
		
		private bool OnlyLinks(List<Wrappers.Wrapper> wrappers)
		{
			return !wrappers.Exists(item => !(item is Wrappers.Edge));
		}
		
		private List<Wrappers.Wrapper> NormalizeSelection(Wrappers.Node parent, Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);
			
			if (parent != null)
			{
				foreach (Wrappers.Wrapper child in parent.Children)
				{
					if (sel.Contains(child) || !(child is Wrappers.Edge))
					{
						continue;
					}
					
					Wrappers.Edge link = (Wrappers.Edge)child;
				
					if (sel.Contains(link.Output) || sel.Contains(link.Input))
					{
						sel.Insert(0, link);
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
					Wrappers.Edge link = wrapper as Wrappers.Edge;
				
					return link != null && !(sel.Contains(link.Output) && sel.Contains(link.Input));
				});
			}
			
			return sel;
		}
		
		private List<Wrappers.Wrapper> NormalizeSelection(Wrappers.Wrapper[] selection)
		{
			return NormalizeSelection(null, selection);
		}
		
		public void Delete(Wrappers.Node parent, Wrappers.Wrapper[] selection)
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
		
		public Wrappers.Node Group(Wrappers.Node parent, Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);

			// Find all the links that go from or to the group, but are not fully in there
			// TODO: automatically create interfaces when needed
			foreach (Wrappers.Edge link in Utils.FilterLink(parent.Children))
			{
				bool containsTo = sel.Contains(link.Output);
				bool containsFrom = sel.Contains(link.Input);

				if (containsTo != containsFrom)
				{
					throw new Exception(String.Format("Links outside the group are acting on different objects in the group. The current behavior of the network cannot be preserved."));
				}
				else if (containsTo && containsFrom && !sel.Contains(link))
				{
					sel.Add(link);
				}
			}

			// Collect all objects and link fully encapsulated in the group
			List<Wrappers.Wrapper> ingroup = new List<Wrappers.Wrapper>();
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper wrapper in sel)
			{
				Wrappers.Edge link = wrapper as Wrappers.Edge;
				
				if (link == null || (sel.Contains(link.Output) && sel.Contains(link.Input)))
				{
					ingroup.Add(wrapper);
				}
				
				// Also fill the first actions that remove all the objects from the parent
				actions.Add(new Undo.RemoveObject(parent, wrapper));
			}
		
			// After objects are removed, we create a new group
			Point xy;
			
			xy = Utils.MeanPosition(ingroup);

			Wrappers.Node newGroup = new Wrappers.Node();
			newGroup.Allocation.X = (int)xy.X;
			newGroup.Allocation.Y = (int)xy.Y;
			
			actions.Add(new Undo.AddObject(parent, newGroup));
			
			// Then we add all the 'ingroup' objects to the group
			foreach (Wrappers.Wrapper wrapper in ingroup)
			{
				// Move object to center at 0, 0 in the group
				if (!(wrapper is Wrappers.Edge))
				{
					actions.Add(new Undo.MoveObject(wrapper, -(int)xy.X, -(int)xy.Y));
				}
				
				// Add object to the group
				actions.Add(new Undo.AddObject(newGroup, wrapper));
			}

			Do(new Undo.AddNode(newGroup, actions));
			return newGroup;
		}
		
		public void Ungroup(Wrappers.Node parent, Wrappers.Wrapper[] selection)
		{
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper wrapper in selection)
			{
				Wrappers.Node grp = wrapper as Wrappers.Node;
				
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
			
			foreach (Wrappers.Edge link in Utils.FilterLink(grp.Parent.Children))
			{
				if (link.Input == grp)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private Undo.IAction[] Ungroup(Wrappers.Node parent, Wrappers.Node grp)
		{
			// Check if links can be redirected to the proxy
			if (HasLinks(grp))
			{
				throw new Exception(String.Format("The group `{0}' has links but no proxy to redirect the links to when ungrouping", grp.Id));
			}

			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			// Remove all objects from the group
			foreach (Wrappers.Wrapper wrapper in grp.Children)
			{
				actions.Add(new Undo.RemoveObject(grp, wrapper));
			}
			
			// Remove the group itself
			actions.Add(new Undo.RemoveObject(parent, grp));
			
			Point cxy = Utils.MeanPosition(grp.Children);
			
			int dx = (int)(grp.Allocation.X - cxy.X);
			int dy = (int)(grp.Allocation.Y - cxy.Y);
			
			foreach (Wrappers.Wrapper wrapper in grp.Children)
			{
				if (!(wrapper is Wrappers.Edge))
				{
					// Center the objects around the position of the group
					actions.Add(new Undo.MoveObject(wrapper, dx, dy));
				}
				
				// Add object to the parent
				actions.Add(new Undo.AddObject(parent, wrapper));
			}
			
			// Reattach any links coming from or going to the group, redirect to proxy
			// TODO: reattach to interfaces

			return actions.ToArray();
		}
		
		private Wrappers.Wrapper[] MakeCopy(Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = NormalizeSelection(selection);
			
			if (sel.Count == 0)
			{
				return new Wrappers.Wrapper[] {};
			}
			
			Dictionary<Cdn.Object, Wrappers.Wrapper> map = new Dictionary<Cdn.Object, Wrappers.Wrapper>();
			List<Wrappers.Wrapper> copied = new List<Wrappers.Wrapper>();
			
			// Create copies and store in a map the mapping from the orig to the copy
			foreach (Wrappers.Wrapper wrapper in sel)
			{
				Wrappers.Wrapper copy = wrapper.Copy();
				
				map[wrapper] = copy;
				copied.Add(copy);
			}
			
			// Reconnect links
			foreach (Wrappers.Edge link in Utils.FilterLink(sel))
			{
				if ((link.Input != null && map.ContainsKey(link.Input)) &&
				    (link.Output != null && map.ContainsKey(link.Output)))
				{
					Wrappers.Node from = map[link.Input] as Wrappers.Node;
					Wrappers.Node to = map[link.Output] as Wrappers.Node;
				
					Wrappers.Edge target = (Wrappers.Edge)map[link.WrappedObject];
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
		
		public void Cut(Wrappers.Node parent, Wrappers.Wrapper[] selection)
		{
			Copy(selection);
			Delete(parent, selection);
		}
		
		public void Paste(Wrappers.Node parent, Wrappers.Wrapper[] selection, int dx, int dy)
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
				List<KeyValuePair<Wrappers.Node, Wrappers.Node>> pairs = new List<KeyValuePair<Wrappers.Node, Wrappers.Node>>(GetLinkPairs(selection));
			
				foreach (Wrappers.Wrapper obj in clip)
				{
					Wrappers.Edge link = (Wrappers.Edge)obj;
					
					foreach (KeyValuePair<Wrappers.Node, Wrappers.Node> pair in pairs)
					{
						Wrappers.Edge copy = (Wrappers.Edge)link.Copy();
						copy.Attach(pair.Key, pair.Value);

						actions.Add(new Undo.AddObject(parent, copy));
					}
				}
			}
			else
			{
				// Paste the new objects by making a copy (yes, again)
				Wrappers.Wrapper[] copied = MakeCopy(Clipboard.Internal.Objects);
			
				Point xy;
				xy = Utils.MeanPosition(copied);
				
				dx -= (int)xy.X;
				dy -= (int)xy.Y;
				
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
			objs.RemoveAll(item => item is Wrappers.Edge && !((Wrappers.Edge)item).Empty);
			
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
		
		public void ApplyTemplate(Wrappers.Wrapper template, Wrappers.Wrapper[] selection)
		{
			if (selection.Length == 0)
			{
				return;
			}

			// Filter to what kind of things we can apply the template
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);

			Type tempType = template.GetType();
			bool isGroup = template is Wrappers.Node;
			
			sel.RemoveAll(delegate (Wrappers.Wrapper item) {
				if (isGroup && item is Wrappers.Object)
				{
					return false;
				}
				
				Type itemType = item.GetType();
				
				return itemType != tempType && !itemType.IsSubclassOf(tempType);
			});
			
			if (sel.Count == 0)
			{
				throw new Exception(String.Format("The template type `{0}' cannot be applied to any of the selected objects", tempType.Name));
			}
			
			List<Undo.IAction> actions = new List<Undo.IAction>();
			
			foreach (Wrappers.Wrapper wrapper in sel)
			{
				actions.Add(new Undo.ApplyTemplate(wrapper, template));
			}
			
			Do(new Undo.Group(actions));
		}
		
		public void UnapplyTemplate(Wrappers.Wrapper obj, Wrappers.Wrapper template)
		{
			Do(new Undo.UnapplyTemplate(obj, template));
		}
		
		public void Do(Undo.IAction action)
		{
			d_undoManager.Do(action);
		}
	}
}

