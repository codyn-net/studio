using System;

namespace Cdn.Studio.Wrappers
{
	public class Import : Node
	{		
		protected Import(Cdn.Import obj) : base(obj)
		{
		}
		
		public Import() : this(null)
		{
		}
		
		public Import(Wrappers.Network network, Wrappers.Node parent, string id, string filename) : this(new Cdn.Import(network, parent, id, filename))
		{
		}
		
		public new Cdn.Import WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.Import;
			}
		}
		
		public static implicit operator Cdn.Import(Wrappers.Import obj)
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