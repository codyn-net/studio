using System;

namespace Cpg.Studio.Wrappers
{
	public class Object : Wrappers.Wrapper
	{
		protected Object(Cpg.Object obj) : base(obj)
		{
			Renderer = new Renderers.State(this);
		}

		public Object() : this("state")
		{
		}
		
		public Object(string id) : this(new Cpg.Object(id))
		{
		}
	}
}

