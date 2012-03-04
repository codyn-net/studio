using System;

namespace Cpg.Studio.Undo
{
	public class ModifyFunctionPolynomialPieceCoefficients : FunctionPolynomialPiece, IAction
	{
		private double[] d_coefficients;
		private double[] d_previousCoefficients;

		public ModifyFunctionPolynomialPieceCoefficients(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece, double[] coefficients) : base(function, piece)
		{
			d_coefficients = coefficients;
			d_previousCoefficients = piece.Coefficients;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Change polynomial piece coefficients on `{0}'",
				                     WrappedObject.FullId);
			}
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

