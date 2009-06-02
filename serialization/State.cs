using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("state")]
	public class State : Simulated
	{
		public State(Components.State state) : base(state)
		{
		}
		
		public State() : this (new Components.State())
		{
		}
	}
}
