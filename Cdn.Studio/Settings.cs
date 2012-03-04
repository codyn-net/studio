using System;
using System.IO;

namespace Cpg.Studio
{
	public class Settings
	{
		public static Pango.FontDescription Font = null;
		private static Plot.Settings s_plotSettings;
		
		public static string PlotSettingsPath
		{
			get
			{
				string cpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				cpath = Path.Combine(cpath, "cpgstudio");
				
				Directory.CreateDirectory(cpath);
				
				return Path.Combine(cpath, "plot-settings.xml");
			}
		}
		
		public static Plot.Settings PlotSettings
		{
			get
			{
				if (s_plotSettings == null)
				{
					string path = PlotSettingsPath;
					
					if (File.Exists(path))
					{
						s_plotSettings = Plot.Settings.Load(PlotSettingsPath);
					}
					else
					{
						s_plotSettings = new Plot.Settings();
					}
				}

				return s_plotSettings;
			}
		}
	}
}
