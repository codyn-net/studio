using System;

namespace Cpg.Studio.Undo
{
	public class RemoveProperty : Property, IAction
	{
		public RemoveProperty(Wrappers.Wrapper wrapped, Cpg.Property property) : base(wrapped, property)
		{
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

