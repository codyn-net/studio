using System;

namespace Cpg.Studio.Undo
{
	public class AddObject : Object, IAction
	{
		public AddObject(Wrappers.Group parent, Wrappers.Wrapper wrapped) : base(parent, wrapped)
		{
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
