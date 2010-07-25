using System;
using System.Collections.Generic;

namespace Cpg.Studio.Interpolators
{
	public class PChip : IInterpolator
	{
		public PChip()
		{
		}
		
		public Interpolation Interpolate(double[] t, double[] p)
		{
			if (t.Length != p.Length || t.Length < 3)
			{
				return null;
			}
			
			int size = t.Length;

			double[] dt = new double[size];
			double[] dpdt = new double[size];
			double[] slopes = new double[size];
			
			for (int i = 0; i < size - 1; ++i)
			{
				double dp = (p[i + 1] - p[i]);
				dt[i] = (t[i + 1] - t[i]);

				dpdt[i] = dt[i] == 0 ? 0 : (dp / dt[i]);
			}
			
			dpdt[size - 1] = 0;
			bool[] samesign = new bool[size - 1];
			
			for (int i = 0; i < size - 2; ++i)
			{
				samesign[i] = (Math.Sign(dpdt[i]) == Math.Sign(dpdt[i + 1])) && dpdt[i] != 0;
			}

			// Three point derivative
			for (int i = 0; i < size - 2; ++i)
			{
				double dpdt1 = dpdt[i];
				double dpdt2 = dpdt[i + 1];
		
				if (samesign[i])
				{
					double hs = dt[i] + dt[i + 1];
		
					double w1 = (dt[i] + hs) / (3 * hs);
					double w2 = (hs + dt[i + 1]) / (3 * hs);
		
					double mindpdt;
					double maxdpdt;
		
					if (dpdt1 > dpdt2)
					{
						maxdpdt = dpdt1;
						mindpdt = dpdt2;
					}
					else
					{
						maxdpdt = dpdt2;
						mindpdt = dpdt1;
					}
		
					slopes[i + 1] = mindpdt / (w1 * (dpdt[i] / maxdpdt) + w2 * (dpdt[i + 1] / maxdpdt));
				}
				else
				{
					slopes[i + 1] = 0;
				}
			}
			
			slopes[0] = ((2 * dt[0] + dt[1]) * dpdt[0] - dt[0] * dpdt[1]) / (dt[0] + dt[1]);

			if (Math.Sign(slopes[0]) != Math.Sign(dpdt[0]))
			{
				slopes[0] = 0;
			}
			else if (Math.Sign(dpdt[0]) != Math.Sign(dpdt[1]) && Math.Abs(slopes[0]) > Math.Abs(3 * dpdt[0]))
			{
				slopes[0] = 3 * dpdt[0];
			}
		
			slopes[size - 1] = ((2 * dt[size - 2] + dt[size - 3]) * dpdt[size - 2] - dt[size - 2] * dpdt[size - 3]) / (dt[size - 2] + dt[size - 3]);
		
			if (Math.Sign(slopes[size - 1]) != Math.Sign(dpdt[size - 2]))
			{
				slopes[size - 1] = 0;
			}
			else if (Math.Sign(dpdt[size - 2]) != Math.Sign(dpdt[size - 3]) && Math.Abs(slopes[size - 1]) > Math.Abs(3 * dpdt[size - 2]))
			{
				slopes[size - 1] = 3 * dpdt[size - 2];
			}
			
			// Generate piece polynomials
			List<Interpolation.Piece> pieces = new List<Interpolation.Piece>();
			
			for (int i = 0; i < size - 1; ++i)
			{
				double s1 = slopes[i];
				double s2 = slopes[i + 1];
				
				double p1 = p[i];
				double p2 = p[i + 1];
				
				double h = t[i + 1] - t[i];
				
				// Order: 0, 1, 2, 3, see definition of hermite spline
				double[] coefficients = new double[] {2 * (p1 - p2) + h * (s1 + s2),
				                                      3 * (p2 - p1) - h * (2 * s1 + s2),
				                                      h * s1,
				                                      p1};
				
				pieces.Add(new Interpolation.Piece(t[i], t[i + 1], coefficients));
			}
			
			return new Interpolation(pieces.ToArray());
		}
		
		public string Name
		{
			get
			{
				return "Hermite Spline (monotonic)";
			}
		}
	}
}
