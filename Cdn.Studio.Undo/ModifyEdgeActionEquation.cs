using System;

namespace Cdn.Studio.Undo
{
	public class ModifyEdgeActionEquation : Object, IAction
	{
		private Wrappers.Edge d_edge;
		private string d_target;
		private string d_equation;
		private string d_previousEquation;

		public ModifyEdgeActionEquation(Wrappers.Edge link, string target, string equation) : base(link.Parent, link)
		{
			d_edge = link;
			
			d_target = target;
			d_equation = equation;
			d_previousEquation = link.GetAction(target).Equation.AsString;
		}

		public string Description
		{
			get
			{
				return String.Format("Change action equation `{0}' to `{1}' on `{2}'", d_previousEquation, d_equation, d_edge.FullId);
			}
		}
		
		public void Undo()
		{
			d_edge.GetAction(d_target).Equation.FromString = d_previousEquation;
		}
		
		public void Redo()
		{
			d_edge.GetAction(d_target).Equation.FromString = d_equation;
		}
	}
}