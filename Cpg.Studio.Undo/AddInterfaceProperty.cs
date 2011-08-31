using System;

namespace Cpg.Studio.Undo
{
	public class AddInterfaceProperty : InterfaceProperty, IAction
	{
		public AddInterfaceProperty(Wrappers.Group grp, string name, string childname, string propid) : base(grp, name, childname, propid)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add interface `{0}' = `{1}.{2}' on `{3}'",
				                     Name,
				                     ChildName,
				                     PropertyId,
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

