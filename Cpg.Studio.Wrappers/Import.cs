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
		
		public Import(Wrappers.Network network, Wrappers.Group parent, string id, string filename) : this(new Cpg.Import(network, parent, id, filename))
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
		
		public bool ImportsObject(Wrappers.Wrapper obj)
		{
			return WrappedObject.ImportsObject(obj);
		}
	}
}