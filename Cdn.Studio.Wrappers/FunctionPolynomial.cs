using System;

namespace Cdn.Studio.Wrappers
{
	public class FunctionPolynomial : Function
	{
		public class PeriodType
		{
			public double Begin;
			public double End;
		}
		
		protected FunctionPolynomial(Cdn.FunctionPolynomial function) : base(function)
		{
			Renderer = new Renderers.PiecewisePolynomial(this);
		}
		
		public FunctionPolynomial(string name) : base(new Cdn.FunctionPolynomial(name))
		{
		}

		public FunctionPolynomial() : this("f")
		{
		}
		
		public new Cdn.FunctionPolynomial WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.FunctionPolynomial;
			}
		}
		
		public static implicit operator Cdn.FunctionPolynomial(Wrappers.FunctionPolynomial obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator FunctionPolynomial(Cdn.FunctionPolynomial obj)
		{
			if (obj == null)
			{
				return null;
			}

			return (FunctionPolynomial)Wrap(obj);
		}
		
		public Cdn.FunctionPolynomialPiece[] Pieces
		{
			get
			{
				return WrappedObject.Pieces;
			}
		}
		
		public void Add(Cdn.FunctionPolynomialPiece piece)
		{
			WrappedObject.Add(piece);
		}
		
		public bool Remove(Cdn.FunctionPolynomialPiece piece)
		{
			return WrappedObject.Remove(piece);
		}
		
		public void ClearPieces()
		{
			WrappedObject.ClearPieces();
		}
	}
}

