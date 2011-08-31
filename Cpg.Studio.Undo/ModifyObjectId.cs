using System;

namespace Cpg.Studio.Undo
{
	public class ModifyObjectId : Object, IAction
	{
		private string d_id;
		private string d_prevId;

		public ModifyObjectId(Wrappers.Wrapper wrapped, string id) : base(wrapped.Parent, wrapped)
		{
			d_id = id;
			d_prevId = wrapped.Id;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change id from `{0}' to `{1}'", d_prevId, d_id);
			}
		}
		
		public void Undo()
		{
			Wrapped.Id = d_prevId;
		}
		
		public void Redo()
		{
			Wrapped.Id = d_id;
		}
	}
}

