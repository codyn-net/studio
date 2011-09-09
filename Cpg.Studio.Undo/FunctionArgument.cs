using System;
using CCpg = Cpg;

namespace Cpg.Studio.Undo
{
	public class FunctionArgument
	{
		private Wrappers.Function d_wrapped;
		private string d_name;
		private string d_defaultValue;
		private bool d_implicit;
		private Cpg.FunctionArgument d_argument;
		
		public FunctionArgument(Wrappers.Function wrapped, string name, string defaultValue, bool isimplicit)
		{
			d_wrapped = wrapped;
			
			d_name = name;
			d_defaultValue = defaultValue;
			d_implicit = isimplicit;
		}
		
		public FunctionArgument(Wrappers.Function wrapped, Cpg.FunctionArgument argument) : this(wrapped, argument.Name, argument.Optional ? argument.DefaultValue.AsString : null, !argument.Explicit)
		{
			d_argument = argument;
		}
		
		public Wrappers.Function Wrapped
		{
			get
			{
				return d_wrapped;
			}
		}
		
		public Cpg.FunctionArgument Argument
		{
			get
			{
				return d_argument;
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
			if (d_defaultValue != null)
			{
				d_wrapped.AddArgument(new CCpg.FunctionArgument(d_name, new Cpg.Expression(d_defaultValue), !d_implicit));
			}
			else
			{
				d_wrapped.AddArgument(new CCpg.FunctionArgument(d_name, null, !d_implicit));
			}
		}
		
		public void Remove()
		{
			foreach (Cpg.FunctionArgument argument in d_wrapped.Arguments)
			{
				if (argument.Name == d_name)
				{
					d_wrapped.RemoveArgument(argument);
					break;
				}
			}
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

