using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionArguments : Function, IAction
	{
		Cpg.FunctionArgument[] d_arguments;
		Cpg.FunctionArgument[] d_prevArguments;

		public ModifyFunctionArguments(Wrappers.Function function, Cpg.FunctionArgument[] arguments) : base(function)
		{
			d_prevArguments = function.Arguments;
			d_arguments = arguments;
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
