using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	public class Object : IComparable
	{
		Wrappers.Wrapper d_object;
		
		public Object(Wrappers.Wrapper obj)
		{
			d_object = obj;
		}
		
		public static implicit operator Object(Wrappers.Wrapper obj)
		{
			return new Object(obj);
		}
		
		public static implicit operator Wrappers.Wrapper(Object obj)
		{
			return obj.WrappedObject;
		}
		
		public Object() : this (new Wrappers.Wrapper())
		{
		}
		
		public T As<T>() where T : Wrappers.Wrapper
		{
			return d_object as T;
		}
		
		public Wrappers.Wrapper WrappedObject
		{
			get
			{
				return d_object;
			}
		}

		public int CompareTo(object other)
		{
			Type[] order = new Type[] {typeof(State), typeof(Link), typeof(Group)};
			
			foreach (Type t in order)
			{
				if (t.Equals(GetType()))
				{
					return (t.Equals(other.GetType()) ? 0 : -1);
				}
				else if (t.Equals(other.GetType()))
				{
				    return 1;
				}
			}
				    
			return 0;
		}
		
		[XmlElement("allocation")]		
		public Allocation Allocation
		{
			get
			{
				return d_object.Allocation;
			}
			set
			{
				d_object.Allocation = value;
			}
		}
		
				
		[XmlAttribute("id")]
		public virtual string Id
		{
			get
			{
				return d_object.Id;
			}
			set
			{
				d_object.Id = value;
			}
		}
		
		[XmlIgnore()]
		public Wrappers.Wrapper Obj
		{
			get
			{
				return d_object;
			}
		}
		
		[XmlElement("property")]
		public Property[] Properties
		{
			get
			{
				Wrappers.Wrapper simulated = As<Wrappers.Wrapper>();
				Cpg.Property[] props = simulated.Properties;
				Property[] properties = new Property[props.Length];
				
				for (int i = 0; i < props.Length; ++i)
				{
					properties[i] = new Property(props[i]);
				}
				
				return properties;
			}
			set
			{
				// TODO
			}
		}

		public static Object Create(Wrappers.Wrapper towrap)
		{
			Type t = towrap.GetType();
			
			if (t == typeof(Wrappers.Link))
			{
				return new Link(towrap as Wrappers.Link);
			}
			else if (t == typeof(Wrappers.Group))
			{
				return new Group(towrap as Wrappers.Group);
			}
			else if (t == typeof(Wrappers.State))
			{
				return new State(towrap as Wrappers.State);
			}
			else
			{
				return null;
			}
		}
	}
}
