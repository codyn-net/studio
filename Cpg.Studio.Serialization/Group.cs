using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	[XmlType("group")]
	public class Group : Object
	{
		List<Serialization.Object> d_children;
		
		public Group(Wrappers.Group group) : base(group)
		{
		}
		
		public Group() : this(new Wrappers.Group())
		{
		}
		
		[XmlAttribute("x")]
		public int X
		{
			get
			{
				Wrappers.Group group = As<Wrappers.Group>();
				
				return group.X;
			}
			set
			{
				Wrappers.Group group = As<Wrappers.Group>();
				group.X = value;
			}
		}
		
		[XmlAttribute("y")]
		public int Y
		{
			get
			{
				Wrappers.Group group = As<Wrappers.Group>();
				
				return group.Y;
			}
			set
			{
				Wrappers.Group group = As<Wrappers.Group>();
				group.Y = value;
			}
		}
		
		public static implicit operator Wrappers.Group(Group group)
		{
			return group.WrappedObject;
		}
		
		public static implicit operator Group(Wrappers.Group group)
		{
			return new Group(group);
		}
		
		public new Wrappers.Group WrappedObject
		{
			get
			{
				return As<Wrappers.Group>();
			}
		}
		
		/* TODO
		[XmlAttribute("main"),
		 System.ComponentModel.DefaultValue("")]
		public string Main
		{
			get
			{
				Wrappers.Group group = As<Wrappers.Group>();
				
				if (group.Main != null)
				{
					return group.Main.Id;
				}
				else
				{
					return "";
				}
			}
			set
			{
				Wrappers.Group group = As<Wrappers.Group>();
				group.Main.Id = value;
			}
		}*/
		
		[XmlAttribute("renderer"),
		 System.ComponentModel.DefaultValue("Default")]
		public string Renderer
		{
			get
			{
				Wrappers.Group group = As<Wrappers.Group>();
				
				if (group.Renderer != null)
					return Wrappers.Renderers.Renderer.GetName(group.Renderer.GetType());
				else
					return "Default";
			}
			set
			{
				Wrappers.Group group = As<Wrappers.Group>();
				group.RendererType = Wrappers.Renderers.Renderer.FindByName(value, typeof(Wrappers.Renderers.Group));
			}
		}
		
		[XmlElement(typeof(State)),
		 XmlElement(typeof(Link)),
		 XmlElement(typeof(Group))]
		public List<Object> Children
		{
			get
			{
				if (d_children != null)
					return d_children;
					
				Wrappers.Group group = As<Wrappers.Group>();
				
				List<Object> children = new List<Object>();
				
				foreach (Wrappers.Wrapper child in group.Children)
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
			Wrappers.Group group = As<Wrappers.Group>();
			
			foreach (Serialization.Object obj in Children)
			{
				group.Add(obj.Obj);
			}
		}
	}
}
