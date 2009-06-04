using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("globals")]
	public class Globals : Serialization.Simulated
	{
		public Globals() : base(new Components.Simulated())
		{
		}
		
		public Globals(Components.Globals globals) : base(globals)
		{
		}
		
		[XmlIgnore()]
		public override string Id
		{
			get
			{
				return "Globals";
			}
			set
			{
			}
		}
	}
}
