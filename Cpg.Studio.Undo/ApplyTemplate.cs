using System;

namespace Cpg.Studio.Undo
{
	public class ApplyTemplate : Template
	{
		public ApplyTemplate(Wrappers.Wrapper obj, Wrappers.Wrapper template) : base(obj, template)
		{
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

