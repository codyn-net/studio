using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlRoot("cpg")]
	public class Main
	{
		private Serialization.Network d_network;
		private Project d_project;
		
		public Main()
		{
			d_project = new Project();
		}
		
		public Main(Cpg.Network network)
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
