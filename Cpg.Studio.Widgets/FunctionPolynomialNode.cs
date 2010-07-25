using System;

namespace Cpg.Studio.Widgets
{
	public class FunctionPolynomialNode : GenericFunctionNode
	{
		[NodeColumn(0)]
		public string Name
		{
			get
			{
				return Function.Id;
			}
		}
	}

}

