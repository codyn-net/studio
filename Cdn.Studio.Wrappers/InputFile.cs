using System;

namespace Cdn.Studio.Wrappers
{
	public class InputFile : Wrappers.Input
	{
		protected InputFile(Cdn.InputFile obj) : base(obj)
		{
		}
		
		public InputFile() : this(new Cdn.InputFile("input"))
		{
		}
		
		public new Cdn.InputFile WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.InputFile;
			}
		}
		
		public static implicit operator Cdn.InputFile(Wrappers.InputFile obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator InputFile(Cdn.InputFile obj)
		{
			if (obj == null)
			{
				return null;
			}

			return (InputFile)Wrap(obj);
		}
	}
}

