using System;

namespace Cpg.Studio.Wrappers
{
	public class InputFile : Wrappers.Input
	{
		protected InputFile(Cpg.InputFile obj) : base(obj)
		{
		}
		
		public InputFile() : this(new Cpg.InputFile("input"))
		{
		}
		
		public new Cpg.InputFile WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.InputFile;
			}
		}
		
		public static implicit operator Cpg.InputFile(Wrappers.InputFile obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator InputFile(Cpg.InputFile obj)
		{
			if (obj == null)
			{
				return null;
			}

			return (InputFile)Wrap(obj);
		}
	}
}

