using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("relay")]
	public class Relay : Simulated
	{
		public Relay(Components.Relay relay) : base(relay)
		{
		}
		
		public Relay() : this(null)
		{
		}
	}
}
