using System;
using System.Xml;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("function-polynomial")]
	public class FunctionPolynomial : Object
	{
		public class PeriodType
		{
			[XmlElement("begin")]
			public double Begin;
		
			[XmlElement("end")]
			public double End;
		}

		[XmlElement("period")]
		public PeriodType Period;
		
		public override void Transfer(Wrappers.Wrapper wrapped)
		{
			base.Transfer(wrapped);
			
			Wrappers.FunctionPolynomial poly = (Wrappers.FunctionPolynomial)wrapped;
			
			if (poly.Period != null)
			{
				Period = new PeriodType();
				Period.Begin = poly.Period.Begin;
				Period.End = poly.Period.End;
			}
			else
			{
				Period = null;
			}
		}
		
		public override void Merge(Wrappers.Wrapper wrapped)
		{
			base.Merge(wrapped);
			
			Wrappers.FunctionPolynomial poly = (Wrappers.FunctionPolynomial)wrapped;
			
			if (Period != null)
			{
				poly.Period = new Wrappers.FunctionPolynomial.PeriodType();
				poly.Period.Begin = Period.Begin;
				poly.Period.End = Period.End;
			}
			else
			{
				poly.Period = null;
			}
		}
	}
}

