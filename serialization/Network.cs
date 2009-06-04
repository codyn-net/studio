using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	[XmlType("network")]
	public class Network
	{
		private List<Simulated> d_objects;
		private CCpg.Network d_network;
		
		public Network()
		{
			d_objects = new List<Simulated>();
		}
		
		public Network(CCpg.Network network) : this()
		{
			d_network = network;
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
		
		[XmlElement("globals", typeof(Globals))]
		public Serialization.Globals Globals
		{
			get
			{
				if (d_network != null)
				{
					return new Serialization.Globals(new Components.Globals(d_network.Globals));
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
