using System;

namespace Cdn.Studio.Undo
{
	public class RemoveInterfaceVariable : InterfaceVariable, IAction
	{
		public RemoveInterfaceVariable(Wrappers.Node grp, string name, string childname, string propid) : base(grp, name, childname, propid)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Remove interface `{0}' = `{1}.{2}' from `{3}'",
				                     Name,
				                     ChildName,
				                     VariableId,
				                     Wrapped.FullId);
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
	}
}
