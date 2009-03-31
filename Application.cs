// Main.cs created with MonoDevelop
// User: jesse at 17:50 24-3-2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using System;
using Gtk;

namespace Cpg.Studio
{
	class Application
	{
		private Studio.Window d_window;
		
		public Application()
		{
			d_window = new Studio.Window();
			d_window.Show();
		}
		
		public void run()
		{
			Gtk.Application.Run();
		}
		
		public static void Main(string[] args)
		{
			Gtk.Application.Init("Cpg Studio", ref args);
			Studio.Application instance = new Studio.Application();
			
			instance.run();
		}
	}
}