using System;

namespace Cdn.Studio.Undo
{
	public class ModifyFunctionArgumentDefaultValue : FunctionArgument, IAction
	{
		private string d_prevVal;
		private string d_newVal;

		public ModifyFunctionArgumentDefaultValue(Wrappers.Function function, Cdn.FunctionArgument argument, string val) : base(function, argument)
		{
			d_prevVal = argument.Optional ? argument.DefaultValue.AsString : null;
			d_newVal = val;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change function argument default value of `{0}.{1}' from `{2}' to `{3}'", Wrapped.FullId, Argument.Name, d_prevVal, d_newVal);
			}
		}
		
		private Cdn.FunctionArgument Lookup()
		{
			foreach (Cdn.FunctionArgument arg in Wrapped.Arguments)
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
			Lookup().DefaultValue = d_newVal == null ? null : new Cdn.Expression(d_newVal);
		}
		
		public void Undo()
		{
			Lookup().DefaultValue = d_prevVal == null ? null : new Cdn.Expression(d_prevVal);
		}
	}
}

