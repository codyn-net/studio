using System;
using System.Xml;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	public class Object
	{
		[XmlElement("allocation")]
		public Allocation Allocation;
		
		[XmlAttribute("id")]
		public string Id;

		public Object()
		{
			Allocation = new Allocation();
		}
		
		public virtual void Transfer(Wrappers.Wrapper wrapped)
		{
			Allocation = wrapped.Allocation.Copy();
			Id = wrapped.Id;
		}
		
		public virtual void Merge(Wrappers.Wrapper wrapped)
		{
			if (Allocation != null)
			{
				wrapped.Allocation = Allocation.Copy();
			}
		}
	}
}

