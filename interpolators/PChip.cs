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
			if (t.Length != p.Length || t.Length < 2)
			{
				return null;
			}
			
			int size = t.Length;

			double[] m = new double[size];
			double[] dk = new double[size];
			
			// Initialize derivatives such that sgn(d_i) = sgn(d_{i + 1}) = sgn(\delta_i)
			m[0] = dk[0] = (p[1] - p[0]) / (t[1] / t[0]);
			m[size - 1] = dk[size - 1] = (p[size - 1] - p[size - 2]) / (t[size - 1] / t[size - 2]);
			
			// Three point derivatives
			for (int i = 1; i < size - 1; ++i)
			{
				double d1 = t[i] - t[i - 1];
                double d2 = t[i + 1] - t[i];
                
                double p1 = d1 == 0 ? 0 : (p[i] - p[i - 1]) / d1;
                double p2 = d2 == 0 ? 0 : (p[i + 1] - p[i]) / d2;
                
                dk[i] = p2;
                m[i] = 0.5 * (p1 + p2);
			}
			
			// Ensure monoticity
	        for (int i = 1; i < size - 1; ++i)
	        {
                if (dk[i] == 0)
                {
                        m[i] = m[i + 1] = 0;
                }
                else
                {
                    // Restrict position vector (alpha, beta) to circle of radius 3
                    double alpha = m[i] / (dk[i] != 0 ? dk[i] : 1);
                    double beta = m[i + 1] / (dk[i] != 0 ? dk[i] : 1);
                    
                    if (alpha + beta - 2 > 0 &&
                        alpha + 2 * beta - 3 > 0 &&
                        alpha - ((2 * alpha + beta - 3) * (2 * alpha + beta - 3)) / 3 * (alpha + beta - 2) < 0)
                    {
                            double tk = 3.0 / System.Math.Sqrt(alpha * alpha + beta * beta);
                            m[i] = tk * alpha * dk[i];
                            m[i + 1] = tk * beta * dk[i];
                    }
                }
	        }

			// Generate piece polynomials
			List<Interpolation.Piece> pieces = new List<Interpolation.Piece>();
			
			for (int i = 0; i < size - 1; ++i)
			{
				double m1 = m[i];
				double m2 = m[i + 1];
				
				double p1 = p[i];
				double p2 = p[i + 1];
				
				double h = t[i + 1] - t[i];
				
				// Order: 0, 1, 2, 3, see definition of hermite spline
				double[] coefficients = new double[] {2 * (p1 - p2) + h * (m1 + m2),
				                                      3 * (-p1 + p2) - h * (2 * m1 + m2),
				                                      h * m1,
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
