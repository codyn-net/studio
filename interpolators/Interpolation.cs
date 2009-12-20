using System;
using System.Collections.Generic;

namespace Cpg.Studio.Interpolators
{
	public class Interpolation
	{
		public class Piece
		{
			private double d_begin;
			private double d_end;
			private double[] d_coefficients;
			private double d_coefficientSum;
			
			public Piece(double begin, double end, double[] coefficients)
			{
				d_begin = begin;
				d_end = end;
				Coefficients = coefficients;
			}
			
			public double Begin
			{
				get { return d_begin; }
				set { d_begin = value; }
			}
			
			public double End
			{
				get { return d_end; }
				set { d_end = value; }
			}
			
			public double[] Coefficients
			{
				get { return d_coefficients; }
				set
				{
					d_coefficients = value;

					d_coefficientSum = 0;
				
					foreach (double coef in d_coefficients)
					{
						d_coefficientSum += coef;
					}
				}
			}
			
			public double CoefficientSum
			{
				get { return d_coefficientSum; }
			}
		}
		
		private List<Piece> d_pieces;
		
		public Interpolation(params Piece[] pieces)
		{
			d_pieces = new List<Piece>(pieces);
		}
		
		public List<Piece> Pieces
		{
			get
			{
				return d_pieces;
			}
		}
		
		public double Evaluate(double t)
		{
			double sum = 0;
			int num = 0;
			
			foreach (Piece piece in d_pieces)
			{
				if (t < piece.Begin || t > piece.End)
				{
					continue;
				}
				
				double norm;
				
				if (piece.End == 0)
				{
					norm = 0;
				}
				else
				{
					norm = (t - piece.Begin) / (piece.End - piece.Begin);
				}

				double tt = 1;
				
				for (int i = piece.Coefficients.Length - 1; i >= 0; --i)
				{
					sum += piece.Coefficients[i] * tt;
					tt *= norm;
				}
		
				++num;
			}
			
			return sum / (num > 0 ? num : 1);
		}
	}
}
