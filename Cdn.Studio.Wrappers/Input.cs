using System;

namespace Cpg.Studio.Wrappers
{
	public abstract class Input : Wrappers.Object
	{
		protected Input(Cpg.Input obj) : base(obj)
		{
			Renderer = new Renderers.Input(this);
		}

		public new Cpg.Input WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Input;
			}
		}
		
		public static implicit operator Cpg.Input(Wrappers.Input obj)
		{
			return obj.WrappedObject;
		}
	}
}

