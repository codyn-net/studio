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
	}
}

