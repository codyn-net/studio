using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	public class Object : IComparable
	{
		Components.Object d_object;
		
		public Object(Components.Object obj)
		{
			d_object = obj;
		}
		
		public Object() : this(null)
		{
		}
		
		public T As<T>() where T : Components.Object
		{
			return d_object as T;
		}

		public int CompareTo(object other)
		{
			Type[] order = new Type[] {typeof(State), typeof(Relay), typeof(Link), typeof(Group)};
			
			foreach (Type t in order)
			{
				if (t.Equals(GetType()))
					return (t.Equals(other.GetType()) ? 0 : -1);
				else if (t.Equals(other.GetType()))
				    return 1;
			}
				    
			return 0;
		}
		
		[XmlElement("allocation")]		
		public Allocation Allocation
		{
			get
			{
				return new Allocation(d_object.Allocation);
			}
			set
			{
				// TODO
			}
		}
		
		public static Object Create(Components.Object towrap)
		{
			Type t = towrap.GetType();
			
			if (t == typeof(Components.Relay))
				return new Relay(towrap as Components.Relay);
			else if (t == typeof(Components.Link))
				return new Link(towrap as Components.Link);
			else if (t == typeof(Components.Group))
				return new Group(towrap as Components.Group);
			else if (t == typeof(Components.State))
				return new State(towrap as Components.State);
			else
				return null;
		}
	}
}
