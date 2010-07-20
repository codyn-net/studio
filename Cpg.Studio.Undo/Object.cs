using System;

namespace Cpg.Studio.Undo
{
	public class Object
	{
		private Wrappers.Group d_parent;
		private Wrappers.Wrapper d_wrapped;

		public Object(Wrappers.Group parent, Wrappers.Wrapper wrapped)
		{
			d_parent = parent;
			d_wrapped = wrapped;
		}
		
		protected void DoAdd()
		{
			if (d_wrapped is Wrappers.Link)
			{
				Wrappers.Link link = (Wrappers.Link)d_wrapped;
				
				link.Reattach();
			}

			d_parent.Add(d_wrapped);
		}
		
		protected void DoRemove()
		{
			d_parent.Remove(d_wrapped);
			d_wrapped.Removed();
		}
		
		public Wrappers.Wrapper Wrapped
		{
			get
			{
				return d_wrapped;
			}
		}
		
		public virtual bool CanMerge(IAction other)
		{
			return false;
		}
		
		public virtual void Merge(IAction other)
		{
		}
	}
}
