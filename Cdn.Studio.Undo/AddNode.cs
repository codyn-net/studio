using System;
using System.Collections.Generic;

namespace Cdn.Studio.Undo
{
	public class AddNode : Group
	{
		private Wrappers.Node d_node;

		public AddNode(Wrappers.Node node, IEnumerable<IAction> actions) : base(actions)
		{
			d_node = node;
		}
		
		public Wrappers.Node Node
		{
			get
			{
				return d_node;
			}
		}
	}
}

