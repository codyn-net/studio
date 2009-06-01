using System;

namespace Cpg.Studio.Components
{
	public class Relay : Simulated
	{
		public Relay(Cpg.Relay obj) : base(obj)
		{
			Renderer = new Renderers.Relay(this);
		}
		
		public Relay() : this(new Cpg.Relay("relay"))
		{
		}
	}
}
