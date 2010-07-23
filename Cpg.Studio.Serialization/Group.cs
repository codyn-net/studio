using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("group")]
	public class Group : Object
	{
		[XmlAttribute("x")]
		public int X;
		
		[XmlAttribute("y")]
		public int Y;
		
		[XmlAttribute("zoom")]
		public int Zoom;
		
		private List<Object> d_children;

		public Group()
		{
			X = 0;
			Y = 0;
			Zoom = Widgets.Grid.DefaultZoom;
			
			d_children = new List<Object>();
		}
		
		[XmlElement(typeof(State)),
		 XmlElement(typeof(Link)),
		 XmlElement(typeof(Group))]
		public List<Object> Children
		{
			get
			{
				return d_children;
			}
			set
			{
				d_children = value;
			}
		}
	}
}

