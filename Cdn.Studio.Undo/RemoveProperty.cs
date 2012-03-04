using System;

namespace Cdn.Studio.Undo
{
	public class RemoveProperty : Property, IAction
	{
		public RemoveProperty(Wrappers.Wrapper wrapped, Cdn.Property property) : base(wrapped, property)
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
			Wrapped.WrappedObject.VerifyRemoveProperty(Name);
			return true;
		}
	}
}

