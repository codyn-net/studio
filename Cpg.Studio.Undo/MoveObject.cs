using System;

namespace Cpg.Studio.Undo
{
	public class MoveObject : Object, IAction
	{
		private int d_dx;
		private int d_dy;

		public MoveObject(Wrappers.Wrapper wrapped, int dx, int dy) : base(null, wrapped)
		{
			d_dx = dx;
			d_dy = dy;
		}
		
		public void Undo()
		{
			Wrapped.MoveRel(-d_dx, -d_dy);
		}
		
		public void Redo()
		{
			Wrapped.MoveRel(d_dx, d_dy);
		}
		
		public override bool CanMerge(IAction other)
		{
			return (other is MoveObject);
		}
		
		public override void Merge(IAction other)
		{
			MoveObject move = (MoveObject)other;

			d_dx += move.d_dx;
			d_dy += move.d_dy;
		}
	}
}
