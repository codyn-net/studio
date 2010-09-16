using System;

namespace Cpg.Studio.Wrappers
{
	public class Import : Group
	{		
		protected Import(Cpg.Import obj) : base(obj)
		{
		}
		
		public Import() : this(null)
		{
		}
		
		public new Cpg.Import WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Import;
			}
		}
		
		public static implicit operator Cpg.Import(Wrappers.Import obj)
		{
			return obj.WrappedObject;
		}
		
		public string Path
		{
			get
			{
				return WrappedObject.Path;
			}
		}
	}
}