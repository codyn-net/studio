using System;

namespace Cpg.Studio.Undo
{
	public class UnapplyTemplate : Template
	{
		public UnapplyTemplate(Wrappers.Wrapper obj, Wrappers.Wrapper template) : base(obj, template)
		{
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

