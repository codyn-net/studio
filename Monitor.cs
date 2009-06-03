using System;
using Gtk;

namespace Cpg.Studio
{
	public class Monitor : Gtk.Window
	{
		Simulation d_simulation;
		bool d_linkRulers;
		bool d_linkAxis;
		UIManager d_uimanager;
		HPaned d_hpaned;
		
		public Monitor(Simulation simulation) : base("Monitor")
		{
			d_simulation = simulation;
			
			d_simulation.OnStep += DoStep;
			d_simulation.OnPeriodBegin += DoPeriodBegin;
			d_simulation.OnPeriodEnd += DoPeriodEnd;
			
			d_linkRulers = true;
			d_linkAxis = true;
			
			Build();
			
			SetDefaultSize(500, 400);
		}

		private void Build()
		{
			VBox vboxMain = new VBox(false, 0);
			VBox vboxContent = new VBox(false,   3);
			
			BuildMenu();
			
			vboxMain.PackStart(d_uimanager.GetWidget("/menubar"), false, false, 0);
			
			d_hpaned = new HPaned();
			CreateObjectList();
			
			Cpg.Studio.Table table = new Cpg.Studio.Table(1, 1, true);
			table.Expand = TCpg.Studio.Table.Expand.Down;
			table.RowSpacing = 1;
			table.ColumnSpacing = 1;
			
			d_hpaned.Pack1(table, true, true);
			vboxContent.PackStart(d_hpaned);
			
			vboxMain.PackStart(vboxContent, true, true, 0);
			
			Add(vboxMain);
			vboxMain.ShowAll();
		}
		
		private void CreateObjectList()
		{
			
		}
		
		private void BuildMenu()
		{
			d_uimanager = new UIManager();
			ActionGroup ag = new ActionGroup("NormalActions");
			
			ag.Add(new ActionEntry[] {
				new ActionEntry("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry("CloseAction", Gtk.Stock.Close, null, null, null, DoClose),
				new ActionEntry("ViewMenuAction", null, "_View", null, null, null),
			});
			
			ag.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("ViewSelectAction", 
				                       null, 
				                       "Show _object list", 
				                       "<Control>o", 
				                       "Show/hide property selection",
				                       DoSelectToggled)
			});
			
			d_uimanager.InsertActionGroup(ag, 0);
			d_uimanager.AddUiFromResource("monitor-ui.xml");
			
			ag = new ActionGroup("MonitorActions");
			
			ag.Add(new ActionEntry[] {
				new ActionEntry("AutoAxisAction", 
				                null, 
				                "Auto scale axis", 
				                "<Control>r", 
				                "Automatically scale axis for data", 
				                DoAutoAxis)
			});
			
			ag.Add(new ToggleActionEntry[] {
				new ToggleActionEntry("LinkedAxisAction",
				                      null,
				                      "Link axis scales",
				                      "<Control>l",
				                      "Link graph axis scales",
				                      DoLinkAxis,
				                      d_linkAxis)
			});

			uint mid = d_uimanager.NewMergeId();
			
			d_uimanager.AddUi(mid, "/menubar/View/ViewBottom", "AutoAxis", "AutoAxisAction", UIManagerItemType.Menuitem, false);
			d_uimanager.AddUi(mid, "/menubar/View/ViewBottom", "LinkedAxis", "LinkedAxisAction", UIManagerItemType.Menuitem, false);
			
			AddAccelGroup(d_uimanager.AccelGroup);
		}

		void DoAutoAxis(object source, EventArgs args)
		{
		}
		
		void DoLinkAxis(object source, ToggledArgs args)
		{
		}
		
		void DoStep(object source, double timestep)
		{
		}
		
		void DoPeriodBegin(object source, Simulation.Args args)
		{
		}
		
		void DoPeriodEnd(object source, Simulation.Args args)
		{
			
		}
	}
}
