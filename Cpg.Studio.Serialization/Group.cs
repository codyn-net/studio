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
		
		private List<Object> d_children;

		public Group()
		{
			X = 0;
			Y = 0;
			
			d_children = new List<Object>();
		}
		
		[XmlElement("state"),
		 XmlElement("link"),
		 XmlElement("group")]
		public Object[] Children
		{
			get
			{
				return d_children.ToArray();
			}
			set
			{
				d_children.Clear();
				d_children.AddRange(value);
			}
		}
	}
}

