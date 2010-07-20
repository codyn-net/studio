using System;

namespace Cpg.Studio.Undo
{
	public class AttachLink : IAction
	{
		private Wrappers.Wrapper d_from;
		private Wrappers.Wrapper d_prevFrom;
		private Wrappers.Wrapper d_to;
		private Wrappers.Wrapper d_prevTo;
		private Wrappers.Link d_link;

		public AttachLink(Wrappers.Link link, Wrappers.Wrapper from, Wrappers.Wrapper to)
		{
			d_from = from;
			d_to = to;
			d_prevFrom = link.From;
			d_prevTo = link.To;
			d_link = link;
		}
		
		public void Undo()
		{
			d_link.Attach(d_prevTo, d_prevFrom);
		}
		
		public void Redo()
		{
			d_link.Attach(d_to, d_from);
		}
		
		public bool Verify()
		{
			return true;
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

