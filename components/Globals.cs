using System;
using System.Collections.Generic;

namespace Cpg.Studio.Components
{
	public class Globals : Cpg.Studio.Components.Simulated
	{
		public Globals(Cpg.Object obj) : base(obj)
		{
			
		}
		
		public override string[] Properties
		{
			get
			{
				List<string> props = new List<string>(base.Properties);
				
				props.Remove("id");
				props.Remove("t");
				props.Remove("dt");
				
				return props.ToArray();
			}
		}
		
		[PropertyAttribute("id", true, true)]
		public override string Id
		{
			get
			{
				return base.Id;
			}
			set
			{
				// NOOP
			}
		}
		
		public override bool CanIntegrate
		{
			get
			{
				return false;
			}
		}
	}
}
