using System;

namespace Cpg.Studio.Undo
{
	public class Function : Object
	{
		private Wrappers.Function d_function;
			
		public Function(Wrappers.Function function) : base(function != null ? function.Parent : null, function)
		{
			d_function = function;
		}
		
		public Wrappers.Function WrappedObject
		{
			get
			{
				return d_function;
			}
		}
	}
}

