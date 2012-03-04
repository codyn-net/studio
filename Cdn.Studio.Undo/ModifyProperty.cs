using System;

namespace Cdn.Studio.Undo
{
	public class ModifyProperty : Property, IAction
	{
		private string d_expression;
		private string d_previousExpression;

		private Cdn.PropertyFlags d_flags;
		private Cdn.PropertyFlags d_previousFlags;

		public ModifyProperty(Wrappers.Wrapper wrapped, Cdn.Property property, string expression) : base(wrapped, property)
		{
			d_expression = expression == null ? "" : expression;
			d_previousExpression = property.Expression.AsString;
		}
		
		public ModifyProperty(Wrappers.Wrapper wrapped, Cdn.Property property, Cdn.PropertyFlags flags) : base(wrapped, property)
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

