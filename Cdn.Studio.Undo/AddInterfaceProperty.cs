using System;

namespace Cdn.Studio.Undo
{
	public class AddInterfaceProperty : InterfaceVariable, IAction
	{
		public AddInterfaceProperty(Wrappers.Node grp, string name, string childname, string propid) : base(grp, name, childname, propid)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add interface `{0}' = `{1}.{2}' on `{3}'",
				                     Name,
				                     ChildName,
				                     VariableId,
				                     Wrapped.FullId);
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

