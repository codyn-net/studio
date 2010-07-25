using System;

namespace Cpg.Studio.Undo
{
	public class Function
	{
		private Wrappers.Function d_function;
			
		public Function(Wrappers.Function function)
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

		public bool Verify()
		{
			return true;
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

