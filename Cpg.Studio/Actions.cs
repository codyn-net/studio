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
			
			d_undoManager.Do(new Undo.AddObject(parent, state));
		}
		
		private IEnumerable<KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>> GetLinkPairs(Wrappers.Wrapper[] selection)
		{
			List<Wrappers.Wrapper> sel = new List<Wrappers.Wrapper>(selection);

			sel.RemoveAll(item => item is Wrappers.Link);

			Wrappers.Wrapper last = selection[sel.Count - 1];
			
			for (int i = 0; i < sel.Count - 1; ++i)
			{
				yield return new KeyValuePair<Wrappers.Wrapper, Wrappers.Wrapper>(sel[i], last);
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

			d_undoManager.Do(new Undo.Group(actions));
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
			
			d_undoManager.Do(new Undo.Group(actions));
		}
		
		public void Group()
		{
		}
		
		public void Ungroup()
		{
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
			
			d_undoManager.Do(new Undo.Group(actions));
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
			
			d_undoManager.Do(new Undo.Group(actions));
		}
	}
}

