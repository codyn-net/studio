using System;

namespace Cpg.Studio.Undo
{
	public class ModifyLinkActionTarget : Object, IAction
	{
		private Wrappers.Link d_link;
		private string d_oldTarget;
		private string d_newTarget;

		public ModifyLinkActionTarget(Wrappers.Link link, string oldTarget, string newTarget) : base(link.Parent, link)
		{
			d_link = link;
			d_oldTarget = oldTarget;
			d_newTarget = newTarget;
		}
		
		public void Undo()
		{
			d_link.GetAction(d_newTarget).Target = d_oldTarget;
		}
		
		public void Redo()
		{
			d_link.GetAction(d_oldTarget).Target = d_newTarget;
		}
	}
}

