using System;

namespace Cpg.Studio.Undo
{
	public class ModifyProxy : IAction
	{
		private Wrappers.Group d_group;
		private Wrappers.Wrapper d_proxy;
		private Wrappers.Wrapper d_previousProxy;

		public ModifyProxy(Wrappers.Group grp, Wrappers.Wrapper proxy)
		{
			d_group = grp;
			d_proxy = proxy;
			d_previousProxy = grp.Proxy;
		}
		
		public void Undo()
		{
			d_group.SetProxy(d_previousProxy);
		}
		
		public void Redo()
		{
			d_group.SetProxy(d_proxy);
		}
		
		public bool CanMerge(IAction other)
		{
			return false;
		}
		
		public void Merge(IAction other)
		{
		}
	}
}

