using System;

namespace Cdn.Studio.Undo
{
	public class Property
	{
		private Wrappers.Wrapper d_wrapped;
		private string d_name;
		private string d_expression;
		private Cdn.PropertyFlags d_flags;
		private Cdn.Property d_property;
		
		public Property(Wrappers.Wrapper wrapped, string name, string expression, Cdn.PropertyFlags flags)
		{
			d_wrapped = wrapped;
			
			d_name = name;
			d_expression = expression;
			d_flags = flags;
		}
		
		public Property(Wrappers.Wrapper wrapped, Cdn.Property property) : this(wrapped, property.Name, property.Expression.AsString, property.Flags)
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
		
		public Cdn.Property Prop
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
			d_wrapped.AddProperty(d_name, d_expression, d_flags);
		}
		
		public void Remove()
		{
			d_wrapped.RemoveProperty(d_name);
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

