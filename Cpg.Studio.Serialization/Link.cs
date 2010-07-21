using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	[XmlType("link")]
	public class Link : Object
	{	
		public Link(Wrappers.Link link) : base(link)
		{
		}
		
		public Link() : this (new Wrappers.Link())
		{
		}

		public static implicit operator Wrappers.Link(Link link)
		{
			return link.WrappedObject;
		}
		
		public static implicit operator Link(Wrappers.Link link)
		{
			return new Link(link);
		}
		
		public new Wrappers.Link WrappedObject
		{
			get
			{
				return As<Wrappers.Link>();
			}
		}
	}
}
