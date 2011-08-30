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

		public PlotSettings(Window parent, Plot.Settings settings) : base("Plot Settings", parent, DialogFlags.NoSeparator | DialogFlags.DestroyWithParent, Gtk.Stock.Close, Gtk.ResponseType.Close)
		{		
			d_fields = new Dictionary<string, PropertyInfo>();
			
			foreach (PropertyInfo field in typeof(Plot.Settings).GetProperties())
			{
				d_fields[field.Name] = field;
			}
			
			d_settings = settings;
			
			string[,] names = new string[,] {
				{"ShowBox", "Show bounding box"},
				{"ShowAxis", "Show axis"},
				{"ShowXTicks", "Show ticks on x-axis"},
				{"ShowYTicks", "Show ticks on y-axis"},
				{"ShowXTicksLabels", "Show tick labels on x-axis"},
				{"ShowYTicksLabels", "Show tick labels on y-axis"},
				{"ShowGrid", "Show grid"},
				{"ShowRuler", "Show ruler"},
				{"SnapRulerToGrid", "Snap ruler to grid"}
			};
			
			for (int i = 0; i < names.GetUpperBound(0); ++i)
			{
				VBox.PackStart(BoolButton(names[i, 1], names[i, 0]), false, false, 0);
			}
			
			VBox.BorderWidth = 6;
		}
		
		private CheckButton BoolButton(string label, string name)
		{
			CheckButton button = new CheckButton(label);
			
			PropertyInfo info = d_fields[name];
			
			button.Active = (bool)info.GetValue(d_settings, new object[] {});
			
			button.Toggled += delegate {
				info.SetValue(d_settings, button.Active, new object[] {});
			};
			
			button.Show();
			return button;
		}
	}
}

