using System;

namespace Cdn.Studio.Undo
{
	public class ModifyFunctionPolynomialPieceEnd : FunctionPolynomialPiece, IAction
	{
		private double d_end;
		private double d_previousEnd;

		public ModifyFunctionPolynomialPieceEnd(Wrappers.FunctionPolynomial function, Cdn.FunctionPolynomialPiece piece, double end) : base(function, piece)
		{
			d_end = end;
			d_previousEnd = piece.End;
		}

		public string Description
		{
			get
			{
				return String.Format("Change polynomial piece end from `{0}' to `{1}' on `{2}'",
				                     d_previousEnd, d_end, WrappedObject.FullId);
			}
		}
		
		public void Undo()
		{
			Piece.End = d_previousEnd;
		}
		
		public void Redo()
		{
			Piece.End = d_end;
		}
	}
}

