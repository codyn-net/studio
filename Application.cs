// Main.cs created with MonoDevelop
// User: jesse at 17:50 24-3-2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using System;
using Gtk;
using System.IO;

namespace Cpg.Studio
{
	class Application
	{
		private Studio.Window d_window;
		
		private void RegisterNativeIntegrators()
		{
			string path = Environment.GetEnvironmentVariable("CPG_INTEGRATOR_PATH");
			
			if (String.IsNullOrEmpty(path))
			{
				return;
			}
			
			string[] dirs = path.Split(':');
			
			foreach (string dir in dirs)
			{
				string[] files;
				
				try
				{
					files = Directory.GetFiles(dir);
				}
				catch
				{
					continue;
				}
				
				foreach (string file in files)
				{
					if (file.EndsWith(".so") || file.EndsWith(".dll"))
					{
						Console.WriteLine(file);
						DynamicIntegrator dyn = new DynamicIntegrator(file);
						
						dyn.Register();
					}
				}
			}
		}
		
		public Application()
		{
			RegisterNativeIntegrators();
			
			d_window = new Studio.Window();
			d_window.Show();
		}
		
		public void run(string[] args)
		{
			if (args.Length > 0)
			{
				d_window.DoLoadXml(args[0]);
			}
			
			Gtk.Application.Run();
		}
		
		public static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

			Gtk.Application.Init("Cpg Studio", ref args);
			Components.Renderers.Oscillator renderer = new Components.Renderers.Oscillator();
			Gtk.Window.DefaultIconList = new Gdk.Pixbuf[] {
				renderer.Icon(16),
				renderer.Icon(24),
				renderer.Icon(48),
				renderer.Icon(128),
			};

			Studio.Application instance = new Studio.Application();
			
			instance.run(args);
		}
	}
}