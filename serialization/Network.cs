using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("network")]
	public class Network
	{
		private List<Simulated> d_objects;
		
		public Network()
		{
			d_objects = new List<Simulated>();
		}

		[XmlElement(typeof(State)),
		 XmlElement(typeof(Relay)),
		 XmlElement(typeof(Link))]
		public List<Simulated> Objects
		{
			get
			{
				return d_objects;
			}
			set
			{
				d_objects = value;
			}
		}
	}
}
