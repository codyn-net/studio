using System;

namespace Cdn.Studio.Undo
{
	public class ModifyLinkActionEquation : Object, IAction
	{
		private Wrappers.Link d_link;
		private string d_target;
		private string d_equation;
		private string d_previousEquation;

		public ModifyLinkActionEquation(Wrappers.Link link, string target, string equation) : base(link.Parent, link)
		{
			d_link = link;
			
			d_target = target;
			d_equation = equation;
			d_previousEquation = link.GetAction(target).Equation.AsString;
		}

		public string Description
		{
			get
			{
				return String.Format("Change action equation `{0}' to `{1}' on `{2}'", d_previousEquation, d_equation, d_link.FullId);
			}
		}
		
		public void Undo()
		{
			d_link.GetAction(d_target).Equation.FromString = d_previousEquation;
		}
		
		public void Redo()
		{
			d_link.GetAction(d_target).Equation.FromString = d_equation;
		}
	}
}