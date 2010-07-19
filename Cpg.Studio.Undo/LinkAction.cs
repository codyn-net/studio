using System;

namespace Cpg.Studio.Undo
{
	public class LinkAction
	{
		private Wrappers.Link d_link;
		private string d_target;
		private string d_expression;
		
		public LinkAction(Wrappers.Link link, string target, string expression)
		{
			d_link = link;
			d_target = target;
			d_expression = expression;
		}
		
		public void Add()
		{
			d_link.AddAction(d_target, d_expression);
		}
		
		public void Remove()
		{
			d_link.RemoveAction(d_link.GetAction(d_target));
		}
		
		public bool CanMerge(IAction other)
		{
			return false;
		}
		
		public void Merge(IAction other)
		{
		}
	}
}

