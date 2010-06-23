using System;

namespace Cpg.Studio.Wrappers.Renderers
{
	public class Group : Renderer
	{
		protected Wrappers.Group d_group;

		public Group(Wrappers.Wrapper obj) : base (obj)
		{
			d_group = obj as Wrappers.Group;
		}
	}
}