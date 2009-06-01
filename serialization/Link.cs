using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("link")]
	public class Link : Simulated
	{
		[XmlType("action")]
		public class Action
		{
			Components.Link.Action d_action;
			
			public Action(Components.Link.Action action)
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
					// TODO
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
					// TODO
				}
			}
		}
		
		public Link(Components.Link link) : base(link)
		{
		}
		
		public Link() : this(null)
		{
		}
		
		[XmlAttribute("from")]
		public string From
		{
			get
			{
				Components.Link link = As<Components.Link>();
				return link.From.FullId();
			}
			set
			{
				// TODO
			}
		}
		
		[XmlAttribute("to")]
		public string To
		{
			get
			{
				Components.Link link = As<Components.Link>();
				return link.To.FullId();
			}
			set
			{
				// TODO
			}
		}
		
		[XmlElement("action", typeof(Action))]
		public Action[] Actions
		{
			get
			{
				Components.Link link = As<Components.Link>();
				List<Action> actions = new List<Action>();
				
				foreach (Components.Link.Action action in link.Actions)
				{
					actions.Add(new Action(action));
				}
				
				return actions.ToArray();
			}
			set
			{
				// TODO
			}
		}
	}
}
