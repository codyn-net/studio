// Stock.cs created with MonoDevelop
// User: jesse at 19:36 24-3-2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using System;
using System.Collections.Generic;

namespace Cdn.Studio
{
	class Stock
	{
		public static string Node = "cdn-node";
		public static string Edge = "cdn-edge";
		public static string Chain = "cdn-chain";
		public static string ChainBroken = "cdn-chain-broken";
		public static string Group = "cdn-group";
		public static string Ungroup = "cdn-ungroup";
		public static string Function = "cdn-function";
		public static string FunctionPolynomial = "cdn-function-polynomial";
		private static Dictionary<string, Cairo.Surface> s_surfaceCache;
		
		static Gtk.IconSet MakeIcons(Wrappers.Renderers.Renderer renderer)
		{
			return MakeIcons(renderer, null);
		}

		static Gtk.IconSet MakeIcons(Wrappers.Renderers.Renderer renderer, string detail)
		{
			Gtk.IconSet s = new Gtk.IconSet();
			Gtk.IconSource source;
			
			renderer.Detail = detail;
			
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
				new Gtk.StockItem(Stock.Node, "Node", 0, 0, null),
				new Gtk.StockItem(Stock.Edge, "Edge", 0, 0, null),
				new Gtk.StockItem(Stock.Chain, "Chain", 0, 0, null),
				new Gtk.StockItem(Stock.ChainBroken, "Chain Broken", 0, 0, null),
				new Gtk.StockItem(Stock.Group, "Group", 0, 0, null),
				new Gtk.StockItem(Stock.Ungroup, "Ungroup", 0, 0, null),
				new Gtk.StockItem(Stock.Function, "Function", 0, 0, null),
				new Gtk.StockItem(Stock.FunctionPolynomial, "Piecewise Polynomial", 0, 0, null)
			});
			
			Gtk.IconFactory factory = new Gtk.IconFactory();

			factory.Add(Stock.Node, MakeIcons(new Wrappers.Renderers.Node()));
			factory.Add(Stock.Edge, MakeIcons(new Wrappers.Renderers.Edge()));
			factory.Add(Stock.Chain, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("chain.png")));
			factory.Add(Stock.ChainBroken, new Gtk.IconSet(Gdk.Pixbuf.LoadFromResource("chain-broken.png")));
			factory.Add(Stock.Group, MakeIcons(new Wrappers.Renderers.Node(), "group"));
			factory.Add(Stock.Ungroup, MakeIcons(new Wrappers.Renderers.Node(), "ungroup"));
			factory.Add(Stock.Function, MakeIcons(new Wrappers.Renderers.Function()));
			factory.Add(Stock.FunctionPolynomial, MakeIcons(new Wrappers.Renderers.Function()));
			
			factory.AddDefault();
			
			s_surfaceCache = new Dictionary<string, Cairo.Surface>();
		}
		
		public static Gtk.Button SmallButton(string stockid)
		{
			Gtk.Button but = new Gtk.Button();
			Gtk.RcStyle rc = new Gtk.RcStyle();
			
			rc.Ythickness = 0;
			rc.Xthickness = 0;
			
			but.ModifyStyle(rc);
			
			but.Image = new Gtk.Image(stockid, Gtk.IconSize.Menu);
			but.Relief = Gtk.ReliefStyle.None;
			
			return but;
		}
		
		private static string SurfaceId(string stockid, int size)
		{
			return String.Format("{0}x{1}", stockid, size);
		}
		
		public static Cairo.Surface Surface(Cairo.Context context, string stockid, int size)
		{
			string id = SurfaceId(stockid, size);
			Cairo.Surface surf;
			
			if (!s_surfaceCache.TryGetValue(id, out surf))
			{
				Gtk.IconTheme theme = Gtk.IconTheme.Default;
				Gdk.Pixbuf pix = theme.LoadIcon(Gtk.Stock.DialogInfo, size, Gtk.IconLookupFlags.UseBuiltin);
				
				surf = context.Target.CreateSimilar(Cairo.Content.ColorAlpha, pix.Width, pix.Height);
			
				using (Cairo.Context ctx = new Cairo.Context(surf))
				{
					Gdk.CairoHelper.SetSourcePixbuf(ctx, pix, 0, 0);
					ctx.Paint();
				}
				
				s_surfaceCache[id] = surf;
			}
			
			return surf;
		}
		
		public static Gtk.Button CloseButton()
		{
			return SmallButton(Gtk.Stock.Close);
		}
		
		public static Gtk.ToggleButton ChainButton()
		{
			Gtk.ToggleButton but = new Gtk.ToggleButton();
			but.Relief = Gtk.ReliefStyle.None;
			
			Gtk.Image im1 = Gtk.Image.LoadFromResource("chain.png");
			Gtk.Image im2 = Gtk.Image.LoadFromResource("chain-broken.png");
			
			but.Active = false;
			but.Image = im1;
			
			but.Toggled += delegate(object sender, EventArgs e) {
				but.Image = (sender as Gtk.ToggleButton).Active ? im2 : im1;		
			};
			
			return but;
		}
	}
}
