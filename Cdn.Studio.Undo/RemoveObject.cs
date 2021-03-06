using System;

namespace Cdn.Studio.Undo
{
	public class RemoveObject : Object, IAction
	{
		public RemoveObject(Wrappers.Node parent, Wrappers.Wrapper wrapped) : base(parent, wrapped)
		{
		}

		public RemoveObject(Wrappers.Wrapper wrapped) : this(wrapped.Parent, wrapped)
		{
		}

		public string Description
		{
			get
			{
				return String.Format("Remove `{0}'", Wrapped.FullId);
			}
		}

		public void Undo()
		{
			DoAdd();
		}

		public void Redo()
		{
			DoRemove();
		}
	}
}
