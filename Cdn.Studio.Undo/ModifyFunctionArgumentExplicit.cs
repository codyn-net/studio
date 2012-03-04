using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionArgumentExplicit : FunctionArgument, IAction
	{
		private bool d_prevVal;
		private bool d_newVal;

		public ModifyFunctionArgumentExplicit(Wrappers.Function function, Cpg.FunctionArgument argument, bool val) : base(function, argument)
		{
			d_prevVal = argument.Explicit;
			d_newVal = val;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change function argument explicity of `{0}.{1}' from `{2}' to `{3}'", Wrapped.FullId, Argument.Name, d_prevVal, d_newVal);
			}
		}
		
		private Cpg.FunctionArgument Lookup()
		{
			foreach (Cpg.FunctionArgument arg in Wrapped.Arguments)
			{
				if (arg.Name == Argument.Name)
				{
					return arg;
				}
			}
			
			return null;
		}
		
		public void Redo()
		{
			Lookup().Explicit = d_newVal;
		}
		
		public void Undo()
		{
			Lookup().Explicit = d_prevVal;
		}
	}
}

