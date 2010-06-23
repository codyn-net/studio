using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	[XmlType("network")]
	public class Network
	{
		private List<Object> d_objects;
		private CCpg.Network d_network;
		
		public Network()
		{
			d_objects = new List<Object>();
		}
		
		public Network(CCpg.Network network) : this()
		{
			d_network = network;
		}

		[XmlElement(typeof(State)),
		 XmlElement(typeof(Group)),
		 XmlElement(typeof(Link))]
		public List<Object> Objects
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
		
		[XmlIgnore()]
		public CCpg.Network CNetwork
		{
			get
			{
				return d_network;
			}
			set
			{
				d_network = value;
			}
		}
		
		[XmlElement("globals", typeof(Object))]
		public Serialization.Object Globals
		{
			get
			{
				if (d_network != null)
				{
					return new Serialization.Object(new Wrappers.Wrapper(d_network));
				}
				else
				{
					Console.WriteLine("Its null");
					return null;
				}
			}
			set
			{
				// Do nothing, really
			}
		}
	}
}
