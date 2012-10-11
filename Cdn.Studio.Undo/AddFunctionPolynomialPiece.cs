using System;

namespace Cdn.Studio.Undo
{
	public class AddFunctionPolynomialPiece : FunctionPolynomialPiece, IAction
	{
		public AddFunctionPolynomialPiece(Wrappers.FunctionPolynomial function, Cdn.FunctionPolynomialPiece piece) : base(function, piece)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add polynomial piece `{0}:{1}' to `{2}'",
				                     Piece.Begin, Piece.End, WrappedObject.FullId);
			}
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

