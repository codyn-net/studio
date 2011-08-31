using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionPolynomialPieceBegin : FunctionPolynomialPiece, IAction
	{
		private double d_begin;
		private double d_previousBegin;

		public ModifyFunctionPolynomialPieceBegin(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece, double begin) : base(function, piece)
		{
			d_begin = begin;
			d_previousBegin = piece.Begin;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change polynomial piece beginning from `{0}' to `{1}' on `{2}'",
				                     d_previousBegin, d_begin, WrappedObject.FullId);
			}
		}
		
		public void Undo()
		{
			Piece.Begin = d_previousBegin;
		}
		
		public void Redo()
		{
			Piece.Begin = d_begin;
		}
	}
}

