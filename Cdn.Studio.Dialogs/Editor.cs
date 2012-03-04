using System;
using Gtk;

namespace Cpg.Studio.Dialogs
{
	public class Editor : Gtk.Window
	{
		public Editor() : base("Editor")
		{
			SetDefaultSize(400, 300);
			
			ScrolledWindow wd = new ScrolledWindow();
			wd.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			
			wd.Show();
			
			GtkSourceView.SourceLanguage lang = GtkSourceView.SourceLanguageManager.Default.GetLanguage("cpg");
			GtkSourceView.SourceBuffer buffer = new GtkSourceView.SourceBuffer(lang);

			GtkSourceView.SourceView view = new GtkSourceView.SourceView(buffer);
			view.Show();
			
			view.ShowLineNumbers = true;
			view.IndentOnTab = true;
			view.InsertSpacesInsteadOfTabs = true;
			view.IndentWidth = 2;
			view.AutoIndent = true;

			wd.Add(view);
			
			Add(wd);
		}
	}
}

