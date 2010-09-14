using System;

namespace Cpg.Studio.Undo
{
	public class MoveObject : Object, IAction
	{
		private int d_dx;
		private int d_dy;
		
		private Group d_mergedGroup;

		public MoveObject(Wrappers.Wrapper wrapped, int dx, int dy) : base(null, wrapped)
		{
			d_dx = dx;
			d_dy = dy;
			
			// The merged group stuff is to support merging with a group whos first action
			// is also a move. This is kind of an ugly hack to make applying a dangling
			// link using DND more usable
			d_mergedGroup = null;
		}
		
		public void Undo()
		{
			if (d_mergedGroup != null)
			{
				d_mergedGroup.Undo();
			}

			Wrapped.MoveRel(-d_dx, -d_dy);
		}
		
		public void Redo()
		{
			Wrapped.MoveRel(d_dx, d_dy);
			
			if (d_mergedGroup != null)
			{
				d_mergedGroup.Redo();
			}
		}
		
		public override bool CanMerge(IAction other)
		{
			MoveObject o = other as MoveObject;
			
			if (o == null)
			{
				Group g = other as Group;
				
				return d_mergedGroup == null && g != null && g.Actions.Count > 0 && g.Actions[0] is MoveObject;
			}
			
			return Wrapped == o.Wrapped;
		}
		
		public override void Merge(IAction other)
		{
			MoveObject move = other as MoveObject;

			if (move != null)
			{
				d_dx += move.d_dx;
				d_dy += move.d_dy;
			}
			else
			{
				d_mergedGroup = other as Group;
			}
		}
	}
}
