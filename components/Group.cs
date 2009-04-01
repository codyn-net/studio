using System;
using System.Collections.Generic;

namespace Cpg.Studio.Components
{
	public class Group : Components.State
	{
		List<Components.Object> d_children;
		int d_x;
		int d_y;
		
		public Group(Cpg.State obj) : base(obj)
		{
			d_children = new List<Components.Object>();
			d_x = 0;
			d_y = 0;
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
		
		public int X
		{
			get
			{
				return d_x;
			}
			set
			{
				d_x = value;
			}
		}
		
		public int Y
		{
			get
			{
				return d_y;
			}
			set
			{
				d_y = value;
			}
		}
	}
}
