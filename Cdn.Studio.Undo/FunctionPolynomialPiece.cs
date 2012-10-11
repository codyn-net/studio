using System;

namespace Cdn.Studio.Undo
{
	public class FunctionPolynomialPiece : Function
	{
		private Cdn.FunctionPolynomialPiece d_piece;

		public FunctionPolynomialPiece(Wrappers.FunctionPolynomial function, Cdn.FunctionPolynomialPiece piece) : base(function)
		{
			d_piece = piece;
		}
		
		public FunctionPolynomialPiece(Cdn.FunctionPolynomialPiece piece) : this(null, piece)
		{
		}
		
		public new Wrappers.FunctionPolynomial WrappedObject
		{
			get
			{
				return (Wrappers.FunctionPolynomial)base.WrappedObject;
			}
		}
		
		public Cdn.FunctionPolynomialPiece Piece
		{
			get
			{
				return d_piece;
			}
		}
		
		protected void DoAddPiece()
		{
			WrappedObject.Add(d_piece);
		}
		
		protected void DoRemovePiece()
		{
			WrappedObject.Remove(d_piece);
		}
	}
}

