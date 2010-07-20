using System;

namespace Cpg.Studio.Undo
{
	public class Property
	{
		private Cpg.Property d_property;
		private Wrappers.Wrapper d_wrapped;
		
		public Property(Wrappers.Wrapper wrapped, Cpg.Property property)
		{
			d_property = property;
			d_wrapped = wrapped;
		}
		
		public Cpg.Property CpgProperty
		{
			get
			{
				return d_property;
			}
		}
		
		public Wrappers.Wrapper Wrapped
		{
			get
			{
				return d_wrapped;
			}
		}
		
		public void Add()
		{
			d_wrapped.AddProperty(d_property.Name, d_property.Expression.AsString, d_property.Flags);
		}
		
		public void Remove()
		{
			d_wrapped.RemoveProperty(d_property.Name);
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

