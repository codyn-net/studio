using System;

namespace Cpg.Studio.Undo
{
	public class RemoveFunctionArgument : FunctionArgument, IAction
	{
		public RemoveFunctionArgument(Wrappers.Function function, string name, string defaultValue, bool isimplicit) : base(function, name, defaultValue, isimplicit)
		{
		}
		
		public RemoveFunctionArgument(Wrappers.Function function, Cpg.FunctionArgument argument) : base(function, argument)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Remove function argument `{0}' from `{1}'", Name, Wrapped.FullId);
			}
		}
		
		public void Undo()
		{
			Add();
		}
		
		public void Redo()
		{
			Remove();
		}
	}
}
