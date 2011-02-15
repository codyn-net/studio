using System;

namespace Cpg.Studio.Undo
{
	public class RemoveInterfaceProperty : InterfaceProperty, IAction
	{
		public RemoveInterfaceProperty(Wrappers.Group grp, string name, string propid) : base(grp, name, propid)
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
