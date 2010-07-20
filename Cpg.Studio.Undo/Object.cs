using System;

namespace Cpg.Studio.Undo
{
	public class Object
	{
		private Wrappers.Group d_parent;
		private Wrappers.Wrapper d_wrapped;
		
		private Wrappers.Wrapper d_from;
		private Wrappers.Wrapper d_to;

		public Object(Wrappers.Group parent, Wrappers.Wrapper wrapped)
		{
			d_parent = parent;
			d_wrapped = wrapped;
			
			Wrappers.Link link = wrapped as Wrappers.Link;
			
			if (link != null)
			{
				d_from = link.From;
				d_to = link.To;
			}
		}
		
		protected void DoAdd()
		{
			Wrappers.Link link = d_wrapped as Wrappers.Link;
			
			if (link != null)
			{
				link.Attach(d_from, d_to);
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
		
		public virtual bool Verify()
		{
			return true;
		}
	}
}
