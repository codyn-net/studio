using System;

namespace Cdn.Studio.Wrappers
{
	public class ImportAlias : Group
	{		
		protected ImportAlias(Cdn.ImportAlias obj) : base(obj)
		{
		}
		
		public ImportAlias(Wrappers.Import source) : this(new Cdn.ImportAlias(source))
		{
		}
		
		public new Cdn.ImportAlias WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.ImportAlias;
			}
		}
		
		public static implicit operator Cdn.ImportAlias(Wrappers.ImportAlias obj)
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