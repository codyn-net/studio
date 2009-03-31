using System;
using System.Collections.Generic;

namespace Cpg.Studio.Components
{
	public class Group : State
	{
		List<Components.Object> d_children;
		
		public Group(Grid grid) : base(grid)
		{
			d_children = new List<Components.Object>();
		}
		
		public Group() : this(null)
		{
		}
		
		public List<Components.Object> Children
		{
			get
			{
				return d_children;
			}
		}
		
		public void Add(Components.Object obj)
		{
			d_children.Add(obj);
		}
	}
}
