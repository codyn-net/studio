using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlRoot("cpg")]
	public class Cpg
	{
		private Network d_network;
		private Project d_project;
		
		public Cpg()
		{
			d_network = new Network();
			d_project = new Project();
		}

		[XmlElement("network")]
		public Network Network
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
		
		[XmlElement("project")]
		public Project Project
		{
			get
			{
				return d_project;
			}
			set
			{
				d_project = value;
			}
		}
	}
}
