using System;

namespace Cpg.Studio.Undo
{
	public class ModifyProperty : IAction
	{
		private Wrappers.Wrapper d_object;
		private string d_property;

		private string d_expression;
		private string d_previousExpression;

		private Cpg.PropertyFlags d_flags;
		private Cpg.PropertyFlags d_previousFlags;

		public ModifyProperty(Cpg.Property property, string expression)
		{
			d_object = Wrappers.Wrapper.Wrap(property.Object);
			d_property = property.Name;

			d_expression = expression == null ? "" : expression;
			d_previousExpression = property.Expression.AsString;
		}
		
		public ModifyProperty(Cpg.Property property, Cpg.PropertyFlags flags)
		{
			d_object = Wrappers.Wrapper.Wrap(property.Object);
			d_property = property.Name;
			d_expression = null;

			d_flags = flags;
			d_previousFlags = property.Flags;
		}
		
		private Cpg.Property Property
		{
			get
			{
				return d_object.Property(d_property);
			}
		}
		
		public void Undo()
		{
			if (d_expression != null)
			{
				Property.Expression.FromString = d_previousExpression;
			}
			else
			{
				Property.Flags = d_previousFlags;
			}
		}
		
		public void Redo()
		{
			if (d_expression != null)
			{
				Property.Expression.FromString = d_expression;
			}
			else
			{
				Property.Flags = d_flags;
			}
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

