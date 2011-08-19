using System;

namespace Cpg.Studio.Undo
{
	public class FunctionPolynomialPiece : Function
	{
		private Cpg.FunctionPolynomialPiece d_piece;

		public FunctionPolynomialPiece(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece) : base(function)
		{
			d_piece = piece;
		}
		
		public FunctionPolynomialPiece(Cpg.FunctionPolynomialPiece piece) : this(null, piece)
		{
		}
		
		public new Wrappers.FunctionPolynomial WrappedObject
		{
			get
			{
				return (Wrappers.FunctionPolynomial)base.WrappedObject;
			}
		}
		
		public Cpg.FunctionPolynomialPiece Piece
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

