using System;

namespace Cdn.Studio.Undo
{
	public class ModifyEdgeActionTarget : Object, IAction
	{
		private Wrappers.Edge d_edge;
		private string d_oldTarget;
		private string d_newTarget;

		public ModifyEdgeActionTarget(Wrappers.Edge link, string oldTarget, string newTarget) : base(link.Parent, link)
		{
			d_edge = link;
			d_oldTarget = oldTarget;
			d_newTarget = newTarget;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change action target `{0}' to `{1}' on `{2}'", d_oldTarget, d_newTarget, d_edge.FullId);
			}
		}
		
		public void Undo()
		{
			d_edge.GetAction(d_newTarget).Target = d_oldTarget;
		}
		
		public void Redo()
		{
			d_edge.GetAction(d_oldTarget).Target = d_newTarget;
		}
	}
}

