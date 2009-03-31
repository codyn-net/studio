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
		
		static Stock()
		{
			Gtk.StockManager.Add(new Gtk.StockItem[] {
				new Gtk.StockItem(Stock.State, "State", 0, 0, null),
				new Gtk.StockItem(Stock.Link, "Link", 0, 0, null),
				new Gtk.StockItem(Stock.Sensor, "Sensor", 0, 0, null),
				new Gtk.StockItem(Stock.Relay, "Relay", 0, 0, null),
				new Gtk.StockItem(Stock.Chain, "Chain", 0, 0, null),
				new Gtk.StockItem(Stock.ChainBroken, "Chain Broken", 0, 0, null)
			});
			
			Gtk.IconFactory factory = new Gtk.IconFactory();
			factory.Add(Stock.State, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("state.png")));
			factory.Add(Stock.Link, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("link.png")));
			factory.Add(Stock.Sensor, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("sensor.png")));
			factory.Add(Stock.Relay, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("relay.png")));
			factory.Add(Stock.Chain, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("chain.png")));
			factory.Add(Stock.ChainBroken, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("chain-broken.png")));
			
			factory.AddDefault();
		}
	}
}
