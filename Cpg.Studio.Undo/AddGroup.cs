using System;
using System.Collections.Generic;

namespace Cpg.Studio.Undo
{
	public class AddGroup : Group
	{
		private Wrappers.Group d_group;

		public AddGroup (Wrappers.Group grp, IEnumerable<IAction> actions) : base(actions)
		{
			d_group = grp;
		}
		
		public Wrappers.Group Group
		{
			get
			{
				return d_group;
			}
		}
	}
}

