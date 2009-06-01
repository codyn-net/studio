using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("group")]
	public class Group : Simulated
	{		
		public Group(Components.Group group) : base(group)
		{
		}
		
		public Group() : this(null)
		{
		}
		
		[XmlAttribute("x")]
		public int X
		{
			get
			{
				Components.Group group = As<Components.Group>();
				
				return group.X;
			}
			set
			{
				// TODO
			}
		}
		
		[XmlAttribute("y")]
		public int Y
		{
			get
			{
				Components.Group group = As<Components.Group>();
				
				return group.Y;
			}
			set
			{
			}
		}
		
		[XmlElement(typeof(State)),
		 XmlElement(typeof(Link)),
		 XmlElement(typeof(Relay)),
		 XmlElement(typeof(Group))]
		public List<Object> Children
		{
			get
			{
				Components.Group group = As<Components.Group>();
				
				List<Object> children = new List<Object>();
				
				foreach (Components.Object child in group.Children)
				{
					Object o = Object.Create(child);
					
					if (o != null)
						children.Add(o);
				}
				
				return children;
			}
			set
			{
				// TODO
			}
		}
	}
}
