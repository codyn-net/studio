using System;

namespace Cpg.Studio.Undo
{
	public class AddInterfaceProperty : InterfaceProperty, IAction
	{
		public AddInterfaceProperty(Wrappers.Group grp, string name, string propid) : base(grp, name, propid)
		{
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

