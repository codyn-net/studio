using System;

namespace Cdn.Studio.Wrappers
{
	public class Object : Wrappers.Wrapper
	{
		protected Object(Cdn.Object obj) : base(obj)
		{
			Renderer = new Renderers.Node(this);
		}

		public Object() : this("state")
		{
		}
		
		public Object(string id) : this(new Cdn.Object(id))
		{
		}
	}
}

