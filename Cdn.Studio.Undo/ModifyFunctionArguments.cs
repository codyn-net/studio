using System;

namespace Cdn.Studio.Undo
{
	public class ModifyFunctionArguments : Function, IAction
	{
		Cdn.FunctionArgument[] d_arguments;
		Cdn.FunctionArgument[] d_prevArguments;

		public ModifyFunctionArguments(Wrappers.Function function, Cdn.FunctionArgument[] arguments) : base(function)
		{
			d_prevArguments = function.Arguments;
			d_arguments = arguments;
		}

		public string Description
		{
			get
			{
				return String.Format("Change function arguments of `{0}'", WrappedObject.FullId);
			}
		}
		
		public void Undo()
		{
			WrappedObject.Arguments = d_prevArguments;
		}
		
		public void Redo()
		{
			WrappedObject.Arguments = d_arguments;
		}
	}
}

