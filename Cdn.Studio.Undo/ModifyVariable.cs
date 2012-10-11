using System;

namespace Cdn.Studio.Undo
{
	public class ModifyVariable : Variable, IAction
	{
		private string d_expression;
		private string d_previousExpression;
		private Cdn.VariableFlags d_flags;
		private Cdn.VariableFlags d_previousFlags;

		public ModifyVariable(Wrappers.Wrapper wrapped, Cdn.Variable property, string expression) : base(wrapped, property)
		{
			d_expression = expression == null ? "" : expression;
			d_previousExpression = property.Expression.AsString;
		}
		
		public ModifyVariable(Wrappers.Wrapper wrapped, Cdn.Variable property, Cdn.VariableFlags flags) : base(wrapped, property)
		{
			d_expression = null;

			d_flags = flags;
			d_previousFlags = property.Flags;
		}
		
		public string Description
		{
			get
			{
				if (d_expression != null)
				{
					return String.Format("Modify expression `{0}'", Prop.FullNameForDisplay);
				}
				else
				{
					return String.Format("Modify flags `{0}'", Prop.FullNameForDisplay);
				}
			}
		}
		
		public void Undo()
		{
			if (d_expression != null)
			{
				Prop.Expression.FromString = d_previousExpression;
			}
			else
			{
				Prop.Flags = d_previousFlags;
			}
		}
		
		public void Redo()
		{
			if (d_expression != null)
			{
				Prop.Expression.FromString = d_expression;
			}
			else
			{
				Prop.Flags = d_flags;
			}
		}
	}
}

