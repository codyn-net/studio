using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("group")]
	public class Group : Simulated
	{
		List<Serialization.Object> d_children;
		
		public Group(Components.Group group) : base(group)
		{
		}
		
		public Group() : this(new Components.Group(new Components.Simulated()))
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
				Components.Group group = As<Components.Group>();
				group.X = value;
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
				Components.Group group = As<Components.Group>();
				group.Y = value;
			}
		}
		
		[XmlAttribute("main"),
		 System.ComponentModel.DefaultValue("")]
		public string Main
		{
			get
			{
				Components.Group group = As<Components.Group>();
				
				if (group.Main != null)
					return group.Main.FullId;
				else
					return "";
			}
			set
			{
				Components.Group group = As<Components.Group>();
				group.Main.Id = value;
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
				if (d_children != null)
					return d_children;
					
				Components.Group group = As<Components.Group>();
				
				List<Object> children = new List<Object>();
				
				foreach (Components.Object child in group.Children)
				{
					Object o = Object.Create(child);
					
					if (o != null)
						children.Add(o);
				}
				
				d_children = children;
				return d_children;
			}
		}
		
		public void Transfer()
		{
			Components.Group group = As<Components.Group>();
			
			foreach (Serialization.Object obj in Children)
			{
				group.Add(obj.Obj);
			}
		}
	}
}
