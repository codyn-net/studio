using System;

namespace Cpg.Studio.Undo
{
	public abstract class Template : IAction
	{
		private Wrappers.Wrapper d_obj;
		private Wrappers.Wrapper d_template;

		public Template(Wrappers.Wrapper obj, Wrappers.Wrapper template)
		{
			d_obj = obj;
			d_template = template;
		}
		
		public abstract string Description
		{
			get;
		}
		
		public Wrappers.Wrapper WrappedTemplate
		{
			get
			{
				return d_template;
			}
		}
		
		public Wrappers.Wrapper WrappedObject
		{
			get
			{
				return d_obj;
			}
		}
		
		protected void Apply()
		{
			d_obj.ApplyTemplate(d_template);
		}
		
		protected void Unapply()
		{
			d_obj.UnapplyTemplate(d_template);
		}
		
		public abstract void Undo();
		public abstract void Redo();
		
		public bool CanMerge(IAction action)
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

