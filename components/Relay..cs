using System;

namespace Cpg.Studio.Components
{
	public class Relay : Simulated
	{
		public Relay(Grid grid) : base(grid)
		{
		}
		
		public Relay() : this(null)
		{
		}
	}
}
