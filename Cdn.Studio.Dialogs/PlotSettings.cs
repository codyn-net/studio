using System;
using Gtk;
using System.Reflection;
using System.Collections.Generic;

namespace Cpg.Studio.Dialogs
{
	public class PlotSettings : Dialog
	{
		private Plot.Settings d_settings;
		private Dictionary<string, PropertyInfo> d_fields;
		private Notebook d_notebook;
		private bool d_updating;
		
		private event EventHandler d_update = delegate {};

		public PlotSettings(Window parent, Plot.Settings settings) : base("Plot Settings", parent, DialogFlags.NoSeparator | DialogFlags.DestroyWithParent, Gtk.Stock.Close, Gtk.ResponseType.Close)
		{		
			d_fields = new Dictionary<string, PropertyInfo>();
			
			foreach (PropertyInfo field in typeof(Plot.Settings).GetProperties())
			{
				d_fields[field.Name] = field;
			}
			
			d_settings = settings;
			
			SetDefaultSize(400, 300);
			BorderWidth = 6;
			
			BuildUI();
		}
		
		private Widget BuildShowPage()
		{
			VBox vbox = new VBox(false, 3);

			string[,] names = new string[,] {
				{"ShowBox", "Bounding box"},
				{"ShowAxis", "Axis"},
				{"ShowXTicks", "Ticks on x-axis"},
				{"ShowYTicks", "Ticks on y-axis"},
				{"ShowXTicksLabels", "Tick labels on x-axis"},
				{"ShowYTicksLabels", "Tick labels on y-axis"},
				{"ShowGrid", "Grid"},
				{"ShowRuler", "Ruler"},
				{"ShowRulerAxis", "Ruler axis"},
				{"SnapRulerToData", "Snap ruler to data"}
			};
			
			for (int i = 0; i <= names.GetUpperBound(0); ++i)
			{
				vbox.PackStart(BoolButton(names[i, 1], names[i, 0]), false, false, 0);
			}
			
			vbox.Show();
			
			return vbox;
		}
		
		private Widget BuildColorsPage()
		{
			string[,] names = new string[,] {
				{"BackgroundColor", "Background"},
				{"GridColor", "Grid"},
				{"AxisColor", "Axis"},
				{"AxisLabelColors", "Axis Label"},
				{"RulerColor", "Ruler"},
				{"RulerLabelColors", "Ruler label"}
			};
			
			Table table = new Table((uint)(names.GetUpperBound(0) + 2), 3, false);
			table.Show();
			table.RowSpacing = 3;
			table.ColumnSpacing = 3;
			
			Label lbl;
			
			lbl = new Label("<b>Name</b>");
			lbl.UseMarkup = true;
			lbl.Xalign = 0;
			lbl.Show();
			
			table.Attach(lbl, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 3);
			
			lbl = new Label("<b>Color</b>");
			lbl.UseMarkup = true;
			lbl.Show();
			
			table.Attach(lbl, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 3);
			
			lbl = new Label("<b>Bg</b>");
			lbl.UseMarkup = true;
			lbl.Show();
			
			table.Attach(lbl, 2, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 3);
			
			for (uint i = 0; i <= names.GetUpperBound(0); ++i)
			{
				lbl = new Label(names[i, 1]);
				lbl.Xalign = 0;
				lbl.Show();
				
				table.Attach(lbl, 0, 1, i + 1, i + 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
				
				string name = names[i, 0];
				string bgname = null;
				
				// Color button
				if (!d_fields.ContainsKey(name))
				{
					bgname = name + "Bg";
					name = name + "Fg";
				}
				
				string fg = (string)d_fields[name].GetValue(d_settings, new object[] {});
				
				ColorButton buttonfg = CreateColorButton(fg);				
				table.Attach(buttonfg, 1, 2, i + 1, i + 2, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
				
				buttonfg.ColorSet += delegate(object sender, EventArgs e) {
					if (!d_updating)
					{
						d_fields[name].SetValue(d_settings, HexColor(buttonfg), new object[] {});
					}
				};
				
				d_update += delegate {
					ParseColor(buttonfg, (string)d_fields[name].GetValue(d_settings, new object[] {}));
				};
				
				if (bgname != null)
				{
					string bg = (string)d_fields[bgname].GetValue(d_settings, new object[] {});
				
					ColorButton buttonbg = CreateColorButton(bg);				
					table.Attach(buttonbg, 2, 3, i + 1, i + 2, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
					
					buttonbg.ColorSet += delegate(object sender, EventArgs e) {
						if (!d_updating)
						{
							d_fields[bgname].SetValue(d_settings, HexColor(buttonbg), new object[] {});
						}
					};
					
					d_update += delegate {
						ParseColor(buttonbg, (string)d_fields[bgname].GetValue(d_settings, new object[] {}));
					};
				}
			}
			
			return table;
		}
		
		private string HexColor(ColorButton b)
		{
			Gdk.Color color = b.Color;
			ushort alpha;
			
			if (b.UseAlpha)
			{
				alpha = b.Alpha;
			}
			else
			{
				alpha = 65535;
			}
			
			return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", 
			    	             (int)(color.Red / 65535.0  * 255),
			        	         (int)(color.Green / 65535.0 * 255),
			            	     (int)(color.Blue / 65535.0 * 255),
			            	     (int)(alpha / 65535.0 * 255));
		}
		
		private void ParseColor(ColorButton button, string hex)
		{
			Gdk.Color color = new Gdk.Color(0, 0, 0);
			bool usealpha = false;
			
			if (hex.Length > 7)
			{
				Gdk.Color.Parse(hex.Substring(0, hex.Length - 2), ref color);
				usealpha = true;
			}
			else
			{
				Gdk.Color.Parse(hex, ref color);
			}
			
			button.Color = color;
			button.UseAlpha = usealpha;
			
			if (usealpha)
			{
				button.Alpha = (ushort)(Convert.ToInt32(hex.Substring(hex.Length - 2), 16) / 255.0 * 65535);
			}
		}
		
		private ColorButton CreateColorButton(string hex)
		{
			ColorButton ret = new ColorButton();
			ret.Show();

			ParseColor(ret, hex);

			return ret;
		}
		
		private Alignment PageAlignment(Widget page)
		{
			Alignment ret = new Alignment(0,0, 1, 1);
			ret.Show();
			ret.BorderWidth = 12;
			
			ret.Add(page);
			
			return ret;
		}
		
		private void BuildUI()
		{
			d_notebook = new Notebook();
			d_notebook.Show();
			d_notebook.BorderWidth = 6;
			
			Label lbl;
			
			lbl = new Label("Show");
			lbl.Show();
			
			d_notebook.AppendPage(PageAlignment(BuildShowPage()), lbl);
			
			lbl = new Label("Colors");
			lbl.Show();
			
			d_notebook.AppendPage(PageAlignment(BuildColorsPage()), lbl);
			
			VBox.PackStart(d_notebook, true, true, 0);
			
			Button revert = new Button(Gtk.Stock.RevertToSaved);
			revert.Show();
			
			ActionArea.PackStart(revert, false, false, 0);
			ActionArea.ReorderChild(revert, 0);
			
			revert.TooltipText = "Revert to original settings";
			
			revert.Clicked += delegate {
				d_updating = true;
				d_settings.Revert();
				d_update(this, new EventArgs());
				d_updating = false;
			};
		}
		
		private CheckButton BoolButton(string label, string name)
		{
			CheckButton button = new CheckButton(label);
			
			PropertyInfo info = d_fields[name];
			
			button.Active = (bool)info.GetValue(d_settings, new object[] {});
			
			button.Toggled += delegate {
				if (!d_updating)
				{
					info.SetValue(d_settings, button.Active, new object[] {});
				}
			};
			
			d_update += delegate {
				button.Active = (bool)info.GetValue(d_settings, new object[] {});
			};
			
			button.Show();
			return button;
		}
	}
}

