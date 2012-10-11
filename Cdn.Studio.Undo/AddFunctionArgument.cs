using System;

namespace Cdn.Studio.Undo
{
	public class AddFunctionArgument : FunctionArgument, IAction
	{
		public AddFunctionArgument(Wrappers.Function function, string name, string defaultValue, bool isimplicit) : base(function, name, defaultValue, isimplicit)
		{
		}
		
		public AddFunctionArgument(Wrappers.Function function, Cdn.FunctionArgument argument) : base(function, argument)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add function argument `{0}' to `{1}'", Name, Wrapped.FullId);
			}
		}
		
		public void Undo()
		{
			Remove();
		}
		
		public void Redo()
		{
			Add();
		}
	}
}

