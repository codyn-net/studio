// Main.cs created with MonoDevelop
// User: jesse at 17:50 24-3-2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using System;
using Gtk;
using System.IO;
using Cdn.Studio.Widgets;

namespace Cdn.Studio
{
	class Application
	{
		private Cdn.Studio.Widgets.Window d_window;
		
		private void RegisterNativeIntegrators(string dir)
		{
			string[] files;
			
			try
			{
				files = Directory.GetFiles(dir);
			}
			catch
			{
				return;
			}
			
			foreach (string file in files)
			{
				if (file.EndsWith(".so") || file.EndsWith(".dll"))
				{
					DynamicIntegrator dyn = new DynamicIntegrator(file);
					
					dyn.Register();
				}
			}
		}
		
		private void RegisterNativeIntegrators()
		{
			RegisterNativeIntegrators(Path.Combine(Path.Combine(Config.Lib, "cdnstudio"), "integrators"));

			string path = Environment.GetEnvironmentVariable("CDN_INTEGRATOR_PATH");
			
			if (String.IsNullOrEmpty(path))
			{
				return;
			}
			
			string[] dirs = path.Split(':');
			
			foreach (string dir in dirs)
			{
				RegisterNativeIntegrators(dir);
			}
		}
		
		public Application()
		{
			RegisterNativeIntegrators();
			
			d_window = new Widgets.Window();
			d_window.Show();
		}
		
		public void Run(string[] args)
		{
			if (args.Length > 0)
			{
				d_window.DoLoad(args[0]);
			}
			
			Gtk.Application.Run();
		}
		
		public static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
			GLib.GType.Init();

			Gtk.Application.Init("Cdn Studio", ref args);
			Wrappers.Renderers.Oscillator renderer = new Wrappers.Renderers.Oscillator();

			Gtk.Window.DefaultIconList = new Gdk.Pixbuf[] {
				renderer.Icon(16),
				renderer.Icon(22),
				renderer.Icon(24),
				renderer.Icon(32),
				renderer.Icon(48),
				renderer.Icon(64),
				renderer.Icon(128),
				renderer.Icon(192),
				renderer.Icon(256)
			};

			Studio.Application instance = new Studio.Application();
			instance.Run(args);
			
			Cdn.Studio.Settings.PlotSettings.Save(Cdn.Studio.Settings.PlotSettingsPath);
		}
	}
}