using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	public class Allocation
	{
		Cpg.Studio.Allocation d_allocation;
		
		public Allocation(Cpg.Studio.Allocation allocation)
		{
			d_allocation = allocation;
		}
	}
}
