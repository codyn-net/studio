using System;
using Gtk;
using Cpg.Studio.Widgets;

namespace Cpg.Studio.Dialogs
{
	public class Functions : Dialog
	{
		private Wrappers.Network d_network;
		private Actions d_actions;
		private FunctionsView d_functionsView;
		private PolynomialsView d_polynomialsView;

		public Functions(Actions actions, Widgets.Window parent, Wrappers.Network network)
		{
			d_network = network;
			d_actions = actions;
			
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
			
			d_functionsView = new FunctionsView(d_actions, d_network);
			d_functionsView.Show();

			Alignment align = new Alignment(0, 0, 1, 1);
			align.SetPadding(6, 6, 0, 0);
			align.Show();
			
			align.Add(d_functionsView);

			notebook.AppendPage(align, new Label("Functions"));
			
			d_polynomialsView = new PolynomialsView(d_actions, d_network);
			d_polynomialsView.Show();
			
			align = new Alignment(0, 0, 1, 1);
			align.SetPadding(6, 6, 0, 0);
			align.Show();
			
			align.Add(d_polynomialsView);
			
			notebook.AppendPage(align, new Label("Polynomials"));

			VBox.Add(notebook);
		}
		
		public void Select(Wrappers.Function function)
		{
			if (!(function.WrappedObject is Cpg.FunctionPolynomial))
			{
				d_functionsView.Select(function);	
			}
			else
			{
				//d_polynomialsView.Select(function);
			}
		}
	}
}
