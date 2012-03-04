using System;

namespace Cdn.Studio.Undo
{
	public class RemoveInterfaceProperty : InterfaceProperty, IAction
	{
		public RemoveInterfaceProperty(Wrappers.Group grp, string name, string childname, string propid) : base(grp, name, childname, propid)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Remove interface `{0}' = `{1}.{2}' from `{3}'",
				                     Name,
				                     ChildName,
				                     PropertyId,
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
