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
		private Widgets.Notebook d_notebook;

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
			d_notebook = notebook;

			VBox.Add(notebook);
		}
		
		public void Select(Wrappers.Function function)
		{
			if (!(function.WrappedObject is Cpg.FunctionPolynomial))
			{
				d_notebook.Page = 0;
				d_functionsView.Select(function);	
			}
			else
			{
				d_notebook.Page = 1;
				d_polynomialsView.Select((Wrappers.FunctionPolynomial)function);
			}
		}
		
		public void Select(Wrappers.FunctionPolynomial function, Cpg.FunctionPolynomialPiece piece)
		{
			d_notebook.Page = 1;
			d_polynomialsView.Select(function, piece);
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
		{
			Gdk.ModifierType state = evnt.State & Gtk.Accelerator.DefaultModMask;
			
			if (state == Gdk.ModifierType.Mod1Mask && (evnt.KeyValue >= '1' && evnt.KeyValue <= '2'))
			{
				d_notebook.Page = (int)(evnt.KeyValue - '1');
				return true;
			}
			
			return base.OnKeyPressEvent(evnt);
		}
	}
}
