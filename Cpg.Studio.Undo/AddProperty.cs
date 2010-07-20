using System;

namespace Cpg.Studio.Undo
{
	public class AddProperty : Property, IAction
	{
		public AddProperty(Wrappers.Wrapper wrapped, string name, string expression, Cpg.PropertyFlags flags) : base(wrapped, name, expression, flags)
		{
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

