using System;

namespace Cpg.Studio.Wrappers
{
	public class Function : Object
	{		
		protected Function(Cpg.Function obj) : base(obj)
		{
		}
		
		public Function() : this(new Cpg.Function("f", "0"))
		{
		}
		
		public Function(string name, string expression) : this(new Cpg.Function(name, expression))
		{
		}
		
		public new Cpg.Function WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Function;
			}
		}
		
		public static implicit operator Cpg.Function(Wrappers.Function obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator Function(Cpg.Function obj)
		{
			if (obj == null)
			{
				return null;
			}

			return (Function)Wrap(obj);
		}
		
		public FunctionArgument[] Arguments
		{
			get
			{
				return WrappedObject.Arguments;
			}
			set
			{
				ClearArguments();
				
				foreach (FunctionArgument arg in value)
				{
					AddArgument(arg);
				}
			}
		}
		
		public Cpg.Expression Expression
		{
			get
			{
				return WrappedObject.Expression;
			}
		}
		
		public void AddArgument(FunctionArgument arg)
		{
			WrappedObject.AddArgument(arg);
		}
		
		public void ClearArguments()
		{
			WrappedObject.ClearArguments();
		}
	}
}
