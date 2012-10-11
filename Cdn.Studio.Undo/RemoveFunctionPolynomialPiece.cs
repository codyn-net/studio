using System;

namespace Cdn.Studio.Undo
{
	public class RemoveFunctionPolynomialPiece : FunctionPolynomialPiece, IAction
	{
		public RemoveFunctionPolynomialPiece(Wrappers.FunctionPolynomial function, Cdn.FunctionPolynomialPiece piece) : base(function, piece)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Remove polynomial piece `{0}:{1}' from `{2}'",
				                     Piece.Begin, Piece.End, WrappedObject.FullId);
			}
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

