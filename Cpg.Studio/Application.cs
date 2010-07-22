// Main.cs created with MonoDevelop
// User: jesse at 17:50 24-3-2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using System;
using Gtk;
using System.IO;
using Cpg.Studio.Widgets;

namespace Cpg.Studio
{
	class Application
	{
		private Cpg.Studio.Widgets.Window d_window;
		
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
			RegisterNativeIntegrators(Path.Combine(Path.Combine(Directories.Lib, "cpgstudio"), "integrators"));

			string path = Environment.GetEnvironmentVariable("CPG_INTEGRATOR_PATH");
			
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

			Gtk.Application.Init("Cpg Studio", ref args);
			Wrappers.Renderers.Oscillator renderer = new Wrappers.Renderers.Oscillator();
			Gtk.Window.DefaultIconList = new Gdk.Pixbuf[] {
				renderer.Icon(16),
				renderer.Icon(24),
				renderer.Icon(48),
				renderer.Icon(128),
			};

			Studio.Application instance = new Studio.Application();
			
			instance.Run(args);
		}
	}
}