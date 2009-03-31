using System;

namespace Cpg.Studio.Components
{
	public class State : Simulated
	{
		public State(Grid grid) : base(grid)
		{
		}
		
		public State() : this(null)
		{
		}
	}
}
