using System;

namespace Cdn.Studio.Undo
{
	public class EdgeAction : Object
	{
		private Wrappers.Edge d_edge;
		private string d_target;
		private string d_expression;
		
		public EdgeAction(Wrappers.Edge link, string target, string expression) : base(link.Parent, link)
		{
			d_edge = link;
			d_target = target;
			d_expression = expression;
		}
		
		public Wrappers.Edge Edge
		{
			get
			{
				return d_edge;
			}
		}
		
		public string Target
		{
			get
			{
				return d_target;
			}
		}
		
		public void Add()
		{
			d_edge.AddAction(d_target, new Cdn.Expression(d_expression));
		}
		
		public void Remove()
		{
			d_edge.RemoveAction(d_edge.GetAction(d_target));
		}
	}
}

