using System;

namespace Cdn.Studio.Undo
{
	public class AddObject : Object, IAction
	{
		public AddObject(Wrappers.Group parent, Wrappers.Wrapper wrapped) : base(parent, wrapped)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add `{0}'", Wrapped.FullId);
			}
		}
		
		public void Undo()
		{
			DoRemove();
		}

		public void Redo()
		{
			DoAdd();
		}
	}
}
