using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionArgumentName : FunctionArgument, IAction
	{
		private string d_prevVal;
		private string d_newVal;

		public ModifyFunctionArgumentName(Wrappers.Function function, Cpg.FunctionArgument argument, string val) : base(function, argument)
		{
			d_prevVal = argument.Name;
			d_newVal = val;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change function argument name of `{0}.{1}' from `{2}' to `{3}'", Wrapped.FullId, Argument.Name, d_prevVal, d_newVal);
			}
		}
		
		private Cpg.FunctionArgument Lookup(string name)
		{
			foreach (Cpg.FunctionArgument arg in Wrapped.Arguments)
			{
				if (arg.Name == name)
				{
					return arg;
				}
			}
			
			return null;
		}
		
		public void Redo()
		{
			Lookup(d_prevVal).Name = d_newVal;
		}
		
		public void Undo()
		{
			Lookup(d_newVal).Name = d_prevVal;
		}
	}
}

