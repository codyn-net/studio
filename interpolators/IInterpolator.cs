using System;

namespace Cpg.Studio.Interpolators
{
	public interface IInterpolator
	{
		Interpolation Interpolate(double[] x, double [] y);
		
		string Name
		{
			get;
		}
	}
}
