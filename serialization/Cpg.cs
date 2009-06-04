using System;
using System.Xml.Serialization;
using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	[XmlRoot("cpg")]
	public class Cpg
	{
		private Serialization.Network d_network;
		private Project d_project;
		
		public Cpg()
		{
			d_project = new Project();
		}
		
		public Cpg(CCpg.Network network)
		{
			d_network = new Serialization.Network(network);
			d_project = new Project();
		}

		[XmlElement("network")]
		public Serialization.Network Network
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
