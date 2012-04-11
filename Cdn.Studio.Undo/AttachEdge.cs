using System;

namespace Cdn.Studio.Undo
{
	public class AttachEdge : Object, IAction
	{
		private Wrappers.Node d_from;
		private Wrappers.Node d_prevFrom;
		private Wrappers.Node d_to;
		private Wrappers.Node d_prevTo;
		private Wrappers.Edge d_link;

		public AttachEdge(Wrappers.Edge link, Wrappers.Node from, Wrappers.Node to) : base(link.Parent, link)
		{
			d_from = from;
			d_to = to;
			d_prevFrom = link.Input;
			d_prevTo = link.Output;
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

