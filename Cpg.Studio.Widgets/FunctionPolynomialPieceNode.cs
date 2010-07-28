using System;

namespace Cpg.Studio.Widgets
{
	public class FunctionPolynomialPieceNode : Node
	{
		private Cpg.FunctionPolynomialPiece d_piece;

		public FunctionPolynomialPieceNode(Cpg.FunctionPolynomialPiece piece)
		{
			Piece = piece;
		}

		[NodeColumn(0)]
		public string Begin
		{
			get
			{
				return d_piece.Begin.ToString();
			}
		}
		
		[NodeColumn(1)]
		public string End
		{
			get
			{
				return d_piece.End.ToString();
			}
		}
		
		[NodeColumn(2)]
		public string Coefficients
		{
			get
			{
				return String.Join(", ", Array.ConvertAll<double, string>(d_piece.Coefficients, item => item.ToString()));
			}
		}
		
		[SortColumn(0)]
		public int SortNode(FunctionPolynomialPieceNode other)
		{
			return d_piece.Begin.CompareTo(other.Piece.Begin);
		}
		
		private void OnPieceChanged(object source, GLib.NotifyArgs args)
		{
			EmitChanged();
		}
		
		private void Connect()
		{
			if (d_piece == null)
			{
				return;
			}
			
			d_piece.AddNotification("begin", OnPieceChanged);
			d_piece.AddNotification("end", OnPieceChanged);
			d_piece.AddNotification("coefficients", OnPieceChanged);
		}
		
		private void Disconnect()
		{
			if (d_piece == null)
			{
				return;
			}
			
			d_piece.RemoveNotification("begin", OnPieceChanged);
			d_piece.RemoveNotification("end", OnPieceChanged);
			d_piece.RemoveNotification("coefficients", OnPieceChanged);
		}
		
		[PrimaryKey]
		public Cpg.FunctionPolynomialPiece Piece
		{
			get
			{
				return d_piece;
			}
			set
			{
				Disconnect();
				d_piece = value;
				Connect();
			}
		}		
	}
}

