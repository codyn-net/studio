using System;

namespace Cpg.Studio.Undo
{
	public class Ungroup : Group
	{
		private Wrappers.Group d_parent;
		private Wrappers.Group[] d_groups;

		public Ungroup(Wrappers.Group parent, Wrappers.Group[] groups)
		{
			d_parent = parent;
			d_groups = groups;
		}
	}
}

