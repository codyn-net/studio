using System;
using Gtk;
using Cpg.Studio.Widgets;

namespace Cpg.Studio.Dialogs
{
	public class Functions : Dialog
	{
		private Wrappers.Network d_network;

		public Functions(Widgets.Window parent, Wrappers.Network network)
		{
			d_network = network;
			
			Title = "Functions";
			
			DestroyWithParent = true;
			HasSeparator = false;
			TransientFor = parent;
			
			SetDefaultSize(600, 300);
			
			AddButton(Gtk.Stock.Close, ResponseType.Close);
			
			InitUi();
		}
		
		private void InitUi()
		{
			Notebook notebook = new Notebook();
			notebook.Show();
			notebook.BorderWidth = 6;
			
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
