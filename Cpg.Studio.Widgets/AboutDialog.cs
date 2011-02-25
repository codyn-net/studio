using System;

namespace Cpg.Studio.Widgets
{
	public class AboutDialog : Gtk.AboutDialog
	{
		private static AboutDialog s_instance;

		public static AboutDialog Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = new AboutDialog();
					
					s_instance.Destroyed += delegate {
						s_instance = null;
					};
					
					s_instance.Response += delegate {
						s_instance.Destroy();
					};
				}
			
				return s_instance;
			}
		}

		public AboutDialog()
		{
			Version = Config.Version;
			Authors = new string[] {"Jesse van den Kieboom"};
			Website = "http://biorob.epfl.ch";
			ProgramName = "CPG Studio";
		}
	}
}

