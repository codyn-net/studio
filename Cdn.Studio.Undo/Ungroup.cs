using System;

namespace Cdn.Studio.Undo
{
	public class Ungroup : Group
	{
		private Wrappers.Node d_parent;

		public Ungroup(Wrappers.Node parent, IAction[] actions) : base(actions)
		{
			d_parent = parent;
		}

		public Wrappers.Node Parent
		{
			get
			{
				return d_parent;
			}
		}
	}
}

