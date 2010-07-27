using System;

namespace Cpg.Studio.Undo
{
	public class AddFunctionPolynomialPiece : FunctionPolynomialPiece, IAction
	{
		public AddFunctionPolynomialPiece(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece) : base(function, piece)
		{
		}
		
		public void Undo()
		{
			DoRemovePiece();
		}
		
		public void Redo()
		{
			DoAddPiece();
		}
	}
}

