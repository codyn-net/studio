using System;

namespace Cdn.Studio.Undo
{
	public class RemoveEdgeAction : EdgeAction, IAction
	{
		public RemoveEdgeAction(Wrappers.Edge link, Cdn.EdgeAction action) : base(link, action.Target, action.Equation.AsString)
		{
		}

		public string Description
		{
			get
			{
				return String.Format("Remove action `{0}' from `{0}'", Target, Edge.FullId);
			}
		}

		public void Undo()
		{
			Add();
		}
		
		public void Redo()
		{
			Remove();
		}
	}
}

