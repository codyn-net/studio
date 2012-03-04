using System;

namespace Cpg.Studio.Undo
{
	public class AttachLink : Object, IAction
	{
		private Wrappers.Wrapper d_from;
		private Wrappers.Wrapper d_prevFrom;
		private Wrappers.Wrapper d_to;
		private Wrappers.Wrapper d_prevTo;
		private Wrappers.Link d_link;

		public AttachLink(Wrappers.Link link, Wrappers.Wrapper from, Wrappers.Wrapper to) : base(link.Parent, link)
		{
			d_from = from;
			d_to = to;
			d_prevFrom = link.From;
			d_prevTo = link.To;
			d_link = link;
		}
		
		public string Description
		{
			get
			{
				if (d_from == null)
				{
					return String.Format("Deattach link `{0}'", d_link.FullId);
				}
				else if (d_from == d_to)
				{
					return String.Format("Attach link `{0}' to `{1}'",
					                     d_link.FullId,
					                     d_from.FullId);
				}
				else
				{
					return String.Format("Attach link `{0}' from `{1}' to `{2}'",
					                     d_link.FullId,
					                     d_from.FullId,
					                     d_to.FullId);
				}
			}
		}
		
		public void Undo()
		{
			d_link.Attach(d_prevFrom, d_prevTo);
		}
		
		public void Redo()
		{
			d_link.Attach(d_from, d_to);
		}
	}
}

