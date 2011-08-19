using System;

namespace Cpg.Studio.Undo
{
	public class ModifyProperty : Property, IAction
	{
		private string d_expression;
		private string d_previousExpression;

		private Cpg.PropertyFlags d_flags;
		private Cpg.PropertyFlags d_previousFlags;

		public ModifyProperty(Wrappers.Wrapper wrapped, Cpg.Property property, string expression) : base(wrapped, property)
		{
			d_expression = expression == null ? "" : expression;
			d_previousExpression = property.Expression.AsString;
		}
		
		public ModifyProperty(Wrappers.Wrapper wrapped, Cpg.Property property, Cpg.PropertyFlags flags) : base(wrapped, property)
		{
			d_expression = null;

			d_flags = flags;
			d_previousFlags = property.Flags;
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

