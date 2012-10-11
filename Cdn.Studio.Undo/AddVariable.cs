using System;

namespace Cdn.Studio.Undo
{
	public class AddVariable : Variable, IAction
	{
		public AddVariable(Wrappers.Wrapper wrapped, string name, string expression, Cdn.VariableFlags flags) : base(wrapped, name, expression, flags)
		{
		}
		
		public AddVariable(Wrappers.Wrapper wrapped, Cdn.Variable prop) : this(wrapped, prop.Name, prop.Expression.AsString, prop.Flags)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add property `{0}' in `{1}'", Name, Wrapped.FullId);
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

