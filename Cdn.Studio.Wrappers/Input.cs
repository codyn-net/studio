using System;

namespace Cdn.Studio.Wrappers
{
	public abstract class Input : Wrappers.Object
	{
		protected Input(Cdn.Input obj) : base(obj)
		{
			Renderer = new Renderers.Input(this);
		}

		public new Cdn.Input WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.Input;
			}
		}
		
		public static implicit operator Cdn.Input(Wrappers.Input obj)
		{
			return obj.WrappedObject;
		}
	}
}

