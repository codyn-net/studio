using System;

namespace Cdn.Studio.Undo
{
	public class Variable
	{
		private Wrappers.Wrapper d_wrapped;
		private string d_name;
		private string d_expression;
		private Cdn.VariableFlags d_flags;
		private Cdn.Variable d_property;
		
		public Variable(Wrappers.Wrapper wrapped, string name, string expression, Cdn.VariableFlags flags)
		{
			d_wrapped = wrapped;
			
			d_name = name;
			d_expression = expression;
			d_flags = flags;
		}
		
		public Variable(Wrappers.Wrapper wrapped, Cdn.Variable property) : this(wrapped, property.Name, property.Expression.AsString, property.Flags)
		{
			d_property = property;
		}
		
		public Wrappers.Wrapper Wrapped
		{
			get
			{
				return d_wrapped;
			}
		}
		
		public Cdn.Variable Prop
		{
			get
			{
				return d_property;
			}
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public void Add()
		{
			d_wrapped.AddVariable(d_name, d_expression, d_flags);
		}
		
		public void Remove()
		{
			d_wrapped.RemoveVariable(d_name);
		}
		
		public virtual bool CanMerge(IAction other)
		{
			return false;
		}
		
		public virtual void Merge(IAction other)
		{
		}
		
		public virtual bool Verify()
		{
			return true;
		}
	}
}

