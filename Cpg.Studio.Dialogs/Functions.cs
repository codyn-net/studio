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
			Widgets.Notebook notebook = new Widgets.Notebook();
			notebook.Show();
			notebook.BorderWidth = 6;
			
			FunctionsView fv = new FunctionsView(d_network);
			fv.Show();

			Alignment align = new Alignment(0, 0, 1, 1);
			align.SetPadding(6, 6, 0, 0);
			align.Show();
			
			align.Add(fv);

			notebook.AppendPage(align, new Label("Functions"));
			
			PolynomialsView pv = new PolynomialsView(d_network);
			pv.Show();
			
			align = new Alignment(0, 0, 1, 1);
			align.SetPadding(6, 6, 0, 0);
			align.Show();
			
			align.Add(pv);
			
			notebook.AppendPage(align, new Label("Polynomials"));

			VBox.Add(notebook);
		}
	}
}
