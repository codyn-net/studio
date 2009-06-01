// Stock.cs created with MonoDevelop
// User: jesse at 19:36 24-3-2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace Cpg.Studio
{
	class Stock
	{
		public static string State = "cpg-state";
		public static string Link = "cpg-link";
		public static string Sensor = "cpg-sensor";
		public static string Relay = "cpg-relay";
		public static string Chain = "cpg-chain";
		public static string ChainBroken = "cpg-chain-broken";
		
		static Gtk.IconSet MakeIcons(Components.Renderers.Renderer renderer)
		{
			Gtk.IconSet s = new Gtk.IconSet();
			Gtk.IconSource source;
			
			Gtk.IconSize[] sizes = new Gtk.IconSize[] {
				Gtk.IconSize.Button,
				Gtk.IconSize.Dialog,
				Gtk.IconSize.Dnd,
				Gtk.IconSize.LargeToolbar,
				Gtk.IconSize.Menu,
				Gtk.IconSize.SmallToolbar
			};
			
			for (int i = 0; i < sizes.Length; ++i)
			{
				source = new Gtk.IconSource();
				
				int width, height;
				Gtk.Icon.SizeLookup(sizes[i], out width, out height);
				
				source.Pixbuf = renderer.Icon(width);
				source.Size = sizes[i];
				source.StateWildcarded = true;
				source.DirectionWildcarded = true;
				source.SizeWildcarded = false;
				
				s.AddSource(source);
			}
			
			return s;
		}
		
		static Stock()
		{
			Gtk.StockManager.Add(new Gtk.StockItem[] {
				new Gtk.StockItem(Stock.State, "State", 0, 0, null),
				new Gtk.StockItem(Stock.Link, "Link", 0, 0, null),
				new Gtk.StockItem(Stock.Relay, "Relay", 0, 0, null),
				new Gtk.StockItem(Stock.Chain, "Chain", 0, 0, null),
				new Gtk.StockItem(Stock.ChainBroken, "Chain Broken", 0, 0, null)
			});
			
			Gtk.IconFactory factory = new Gtk.IconFactory();

			factory.Add(Stock.State, MakeIcons(new Components.Renderers.State()));
			factory.Add(Stock.Link, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("link.png")));
			factory.Add(Stock.Relay, MakeIcons(new Components.Renderers.Relay()));
			factory.Add(Stock.Chain, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("chain.png")));
			factory.Add(Stock.ChainBroken, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("chain-broken.png")));
			
			factory.AddDefault();
		}
	}
}
