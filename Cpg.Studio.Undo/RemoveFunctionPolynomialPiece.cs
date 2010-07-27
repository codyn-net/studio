using System;

namespace Cpg.Studio.Undo
{
	public class RemoveFunctionPolynomialPiece : FunctionPolynomialPiece, IAction
	{
		public RemoveFunctionPolynomialPiece(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece) : base(function, piece)
		{
		}
		
		public void Undo()
		{
			DoAddPiece();
		}
		
		public void Redo()
		{
			DoRemovePiece();
		}
	}
}

