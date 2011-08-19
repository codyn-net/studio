using System;

namespace Cpg.Studio.Undo
{
	public class ModifyExpression : IAction
	{
		Cpg.Expression d_expr;
		string d_expression;
		string d_prevExpression;

		public ModifyExpression(Cpg.Expression expr, string expression)
		{
			d_expr = expr;
			d_expression = expression;
			d_prevExpression = expr.AsString;
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

