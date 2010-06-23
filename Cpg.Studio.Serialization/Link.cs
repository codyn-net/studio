using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	[XmlType("link")]
	public class Link : Object
	{
		[XmlType("action")]
		public class Action
		{
			Wrappers.Link.Action d_action;
			
			public Action(Wrappers.Link.Action action)
			{
				d_action = action;
			}
			
			public Action() : this(null)
			{
			}
			
			[XmlAttribute("target")]
			public string Target
			{
				get
				{
					return d_action.Target;
				}
				set
				{
					// NOOP
				}
			}
			
			[XmlText()]
			public string Equation
			{
				get
				{
					return d_action.Equation;
				}
				set
				{
					// NOOP
				}
			}
		}
		
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
		
		[XmlAttribute("from")]
		public string From
		{
			get
			{
				return As<Wrappers.Link>().From.Id;
			}
			set
			{
				// NOOP
			}
		}
		
		[XmlAttribute("to")]
		public string To
		{
			get
			{
				return As<Wrappers.Link>().To.Id;
			}
			set
			{
				// NOOP
			}
		}
		
		[XmlElement("action", typeof(Action))]
		public Action[] Actions
		{
			get
			{
				Wrappers.Link link = As<Wrappers.Link>();
				List<Action> actions = new List<Action>();
				
				foreach (Wrappers.Link.Action action in link.Actions)
				{
					actions.Add(new Action(action));
				}
				
				return actions.ToArray();
			}
			set
			{
				// NOOP
			}
		}
	}
}
