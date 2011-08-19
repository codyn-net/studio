using System;

namespace Cpg.Studio.Wrappers
{
	public class ImportAlias : Group
	{		
		protected ImportAlias(Cpg.ImportAlias obj) : base(obj)
		{
		}
		
		public ImportAlias(Wrappers.Import source) : this(new Cpg.ImportAlias(source))
		{
		}
		
		public new Cpg.ImportAlias WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.ImportAlias;
			}
		}
		
		public static implicit operator Cpg.ImportAlias(Wrappers.ImportAlias obj)
		{
			return obj.WrappedObject;
		}
		
		public Wrappers.Import Source
		{
			get
			{
				return (Wrappers.Import)Wrappers.Wrapper.Wrap(WrappedObject.Source);
			}
		}
	}
}