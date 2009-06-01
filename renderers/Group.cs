using System;

namespace Cpg.Studio.Components.Renderers
{
	public class Group : Renderer
	{
		protected Components.Group d_group;

		public Group(Components.Object obj) : base (obj)
		{
			d_group = obj as Components.Group;
		}
	}
}