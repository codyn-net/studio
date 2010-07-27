using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionPolynomialPieceEnd : FunctionPolynomialPiece, IAction
	{
		private double d_end;
		private double d_previousEnd;

		public ModifyFunctionPolynomialPieceEnd(Cpg.FunctionPolynomialPiece piece, double end) : base(piece)
		{
			d_end = end;
			d_previousEnd = piece.End;
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

