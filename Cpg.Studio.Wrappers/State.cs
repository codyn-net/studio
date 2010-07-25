using System;

namespace Cpg.Studio.Wrappers
{
	public class State : Wrapper
	{		
		protected State(Cpg.State obj) : base(obj)
		{
			Renderer = new Renderers.State(this);
		}
		
		public State() : this(new Cpg.State("state"))
		{
		}
		
		public new Cpg.State WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.State;
			}
		}
		
		public static implicit operator Cpg.State(Wrappers.State obj)
		{
			return obj.WrappedObject;
		}
	}
}
