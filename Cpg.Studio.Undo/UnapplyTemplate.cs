using System;

namespace Cpg.Studio.Undo
{
	public class UnapplyTemplate : Template
	{
		public UnapplyTemplate(Wrappers.Wrapper obj, Wrappers.Wrapper template) : base(obj, template)
		{
		}
		
		public override string Description
		{
			get
			{
				return String.Format("Unapply template `{0}' from `{1}'",
				                     WrappedTemplate.FullId,
				                     WrappedObject.FullId);
			}
		}
		
		public override void Undo()
		{
			Apply();
		}
		
		public override void Redo()
		{
			Unapply();
		}
	}
}

