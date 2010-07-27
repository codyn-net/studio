using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionPolynomialPieceCoefficients : FunctionPolynomialPiece, IAction
	{
		private double[] d_coefficients;
		private double[] d_previousCoefficients;

		public ModifyFunctionPolynomialPieceCoefficients(Cpg.FunctionPolynomialPiece piece, double[] coefficients) : base(piece)
		{
			d_coefficients = coefficients;
			d_previousCoefficients = piece.Coefficients;
		}
		
		public void Undo()
		{
			Piece.Coefficients = d_previousCoefficients;
		}
		
		public void Redo()
		{
			Piece.Coefficients = d_coefficients;
		}
	}
}

