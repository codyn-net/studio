using System;

namespace Cpg.Studio.Components
{
	public class State : Simulated
	{		
		public State(Cpg.State obj) : base(obj)
		{
			Renderer = new Renderers.State(this);
		}
		
		public State() : this(new Cpg.State("state"))
		{
		}
	}
}
