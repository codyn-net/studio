using System;

namespace Cdn.Studio.Undo
{
	public class RemoveVariable : Variable, IAction
	{
		public RemoveVariable(Wrappers.Wrapper wrapped, Cdn.Variable property) : base(wrapped, property)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Remove property `{0}' from `{1}'", Name, Wrapped.FullId);
			}
		}
		
		public void Undo()
		{
			Add();
		}
		
		public void Redo()
		{
			Remove();
		}
		
		public override bool Verify()
		{
			// Will throw an exception
			Wrapped.WrappedObject.VerifyRemoveVariable(Name);
			return true;
		}
	}
}

