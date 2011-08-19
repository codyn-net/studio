using System;

namespace Cpg.Studio.Undo
{
	public class RemoveInterfaceProperty : InterfaceProperty, IAction
	{
		public RemoveInterfaceProperty(Wrappers.Group grp, string name, string childname, string propid) : base(grp, name, childname, propid)
		{
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
