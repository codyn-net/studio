using System;

namespace Cdn.Studio.Undo
{
	public class ModifyExpression : IAction
	{
		Cdn.Expression d_expr;
		string d_expression;
		string d_prevExpression;

		public ModifyExpression(Cdn.Expression expr, string expression)
		{
			d_expr = expr;
			d_expression = expression;
			d_prevExpression = expr.AsString;
		}

		public string Description
		{
			get
			{
				return String.Format("Change expression from `{0}' to `{1}'", d_prevExpression, d_expression);
			}
		}
		
		public void Undo()
		{
			d_expr.FromString = d_prevExpression;
		}
		
		public void Redo()
		{
			d_expr.FromString = d_expression;
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

