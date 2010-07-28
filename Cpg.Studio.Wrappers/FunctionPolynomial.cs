using System;

namespace Cpg.Studio.Wrappers
{
	public class FunctionPolynomial : Function
	{
		public class PeriodType
		{
			public double Begin;
			public double End;
		}
		
		private PeriodType d_period;

		protected FunctionPolynomial(Cpg.FunctionPolynomial function) : base(function)
		{
		}
		
		public FunctionPolynomial(string name) : base(new Cpg.FunctionPolynomial(name))
		{
		}

		public FunctionPolynomial() : this("f")
		{
		}
		
		public new Cpg.FunctionPolynomial WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.FunctionPolynomial;
			}
		}
		
		public static implicit operator Cpg.FunctionPolynomial(Wrappers.FunctionPolynomial obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator FunctionPolynomial(Cpg.FunctionPolynomial obj)
		{
			if (obj == null)
			{
				return null;
			}

			return (FunctionPolynomial)Wrap(obj);
		}
		
		public PeriodType Period
		{
			get
			{
				return d_period;
			}
			set
			{
				d_period = value;
			}
		}
		
		public Cpg.FunctionPolynomialPiece[] Pieces
		{
			get
			{
				return WrappedObject.Pieces;
			}
		}
		
		public void Add(Cpg.FunctionPolynomialPiece piece)
		{
			WrappedObject.Add(piece);
		}
		
		public bool Remove(Cpg.FunctionPolynomialPiece piece)
		{
			return WrappedObject.Remove(piece);
		}
		
		public void ClearPieces()
		{
			WrappedObject.ClearPieces();
		}
	}
}

