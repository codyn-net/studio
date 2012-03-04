using System;

namespace Cpg.Studio.Undo
{
	public class ApplyTemplate : Template
	{
		public ApplyTemplate(Wrappers.Wrapper obj, Wrappers.Wrapper template) : base(obj, template)
		{
		}
		
		public override string Description
		{
			get
			{
				return String.Format("Apply template `{0}' to `{1}'",
				                     WrappedTemplate.FullId,
				                     WrappedObject.FullId);
			}
		}
		
		public override void Undo()
		{
			Unapply();
		}
		
		public override void Redo()
		{
			Apply();
		}
	}
}

