using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("state")]
	public class State : Object
	{
		public State(Wrappers.State state) : base(state)
		{
		}
		
		public State() : this (new Wrappers.State())
		{
		}
		
		public static implicit operator Wrappers.State(State state)
		{
			return state.WrappedObject;
		}
		
		public static implicit operator State(Wrappers.State state)
		{
			return new State(state);
		}
		
		public new Wrappers.State WrappedObject
		{
			get
			{
				return As<Wrappers.State>();
			}
		}
	}
}
