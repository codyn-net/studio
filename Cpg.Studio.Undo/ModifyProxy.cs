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

		public string Description
		{
			get
			{
				return String.Format("Change proxy `{0}' to `{1}' on `{2}'", d_previousProxy.Id, d_proxy.Id, d_group.FullId);
			}
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
		
		public bool Verify()
		{
			return true;
		}
	}
}

