using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	public class Simulated : Object
	{
		public Simulated(Components.Simulated simulated) : base(simulated)
		{
		}
		
		public Simulated()
		{
		}
		
		[XmlAttribute("id")]
		public string Id
		{
			get
			{
				return As<Components.Simulated>().FullId();
			}
			set
			{
				// TODO
			}
		}
		
		[XmlElement("property")]
		public Property[] Properties
		{
			get
			{
				Components.Simulated simulated = As<Components.Simulated>();
				string[] props = simulated.Properties;
				List<Property> properties = new List<Property>();
				
				foreach (string prop in props)
				{
					if (prop != "id")
						properties.Add(new Property(prop, simulated[prop], simulated.GetIntegrated(prop)));
				}
				
				return properties.ToArray();
			}
			set
			{
				// TODO
			}
		}
	}
}
