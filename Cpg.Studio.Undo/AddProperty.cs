using System;

namespace Cpg.Studio.Undo
{
	public class AddProperty : Property, IAction
	{
		public AddProperty(Wrappers.Wrapper wrapped, Cpg.Property property) : base(wrapped, property)
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

