using System;

namespace Cdn.Studio.Undo
{
	public class LinkAction : Object
	{
		private Wrappers.Link d_link;
		private string d_target;
		private string d_expression;
		
		public LinkAction(Wrappers.Link link, string target, string expression) : base(link.Parent, link)
		{
			d_link = link;
			d_target = target;
			d_expression = expression;
		}
		
		public Wrappers.Link Link
		{
			get
			{
				return d_link;
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
			d_link.AddAction(d_target, new Cdn.Expression(d_expression));
		}
		
		public void Remove()
		{
			d_link.RemoveAction(d_link.GetAction(d_target));
		}
	}
}

