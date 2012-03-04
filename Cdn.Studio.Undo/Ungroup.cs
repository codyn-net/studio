using System;

namespace Cdn.Studio.Undo
{
	public class Ungroup : Group
	{
		private Wrappers.Group d_parent;

		public Ungroup(Wrappers.Group parent, IAction[] actions) : base(actions)
		{
			d_parent = parent;
		}

		public Wrappers.Group Parent
		{
			get
			{
				return d_parent;
			}
		}
	}
}

