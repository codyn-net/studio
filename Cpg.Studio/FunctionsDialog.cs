using System;
using Gtk;

namespace Cpg.Studio
{
	public class FunctionsDialog : Dialog
	{
		private Wrappers.Network d_network;

		public FunctionsDialog(Window parent, Wrappers.Network network)
		{
			d_network = network;
			
			Title = "Functions";
			
			DestroyWithParent = true;
			HasSeparator = false;
			TransientFor = parent;
			BorderWidth = 10;
			
			SetDefaultSize(600, 300);
			VBox.Spacing = 6;
			
			AddButton(Gtk.Stock.Close, ResponseType.Close);
			
			InitUi();
		}
		
		private void InitUi()
		{
			Notebook notebook = new Notebook();
			notebook.Show();
			
			FunctionsView fv = new FunctionsView(d_network);
			fv.Show();
			
			notebook.AppendPage(fv, new Label("Functions"));
			
			PolynomialsView pv = new PolynomialsView(d_network);
			pv.Show();
			
			notebook.AppendPage(pv, new Label("Polynomials"));

			VBox.Add(notebook);
		}
	}
}
