using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("project")]
	public class Project
	{
		private Window d_window;
		private Group d_root;
		private int d_zoom;
		private Allocation d_allocation;
		private string d_period;
		private int d_panePosition;
		private string d_container;
		private bool d_showStatusbar;
		private bool d_showToolbar;
		private bool d_showSimulateButtons;
		private bool d_showPathbar;

		public Project(Window window)
		{
			d_window = window;
			
			d_zoom = 50;
			d_period = "";
			d_panePosition = 200;
			
			d_showSimulateButtons = true;
			d_showStatusbar = true;
			d_showToolbar = true;
			d_showPathbar = true;
		}
		
		public Project() : this(null)
		{
		}
		
		[XmlIgnore()]
		public Window Window
		{
			get
			{
				return d_window;
			}
			set
			{
				d_window = value;
			}
		}

		[XmlElement("zoom")]
		public int Zoom
		{
			get
			{
				return d_window != null ? d_window.Grid.GridSize : d_zoom;
			}
			set
			{
				d_zoom = value;
			}
		}
		
		[XmlElement("container"),
		 System.ComponentModel.DefaultValue("")]
		public string Container
		{
			get
			{
				return d_window != null ? d_window.Grid.Container.FullId : d_container;
			}
			set
			{
				d_container = value;
			}
		}
		
		[XmlElement("allocation")]
		public Allocation Allocation
		{
			get
			{
				if (d_allocation != null)
					return d_allocation;
				
				if (d_window == null)
					return null;
				
				int root_x;
				int root_y;
				int width;
				int height;
				
				d_window.GetPosition(out root_x, out root_y);
				d_window.GetSize(out width, out height);
				
				return new Allocation(root_x, root_y, width, height);
			}
			set
			{
				d_allocation = value;
			}
		}
		
		[XmlElement("period"),
		 System.ComponentModel.DefaultValue("")]
		public string Period
		{
			get
			{
				return d_window != null ? d_window.Period : d_period;
			}
			set
			{
				d_period = value;
			}
		}

		[XmlElement("canvas")]
		public Group Root
		{
			get
			{
				return d_root == null ? new Group(d_window.Grid.Root) : d_root;
			}
			set
			{
				d_root = value;
			}
		}
		
		[XmlElement("pane-position")]
		public int PanePosition
		{
			get
			{
				return d_window != null ? d_window.PanePosition : d_panePosition;
			}
			set
			{
				d_panePosition = value;
			}
		}
		
		[XmlElement("show-toolbar")]
		public bool ShowToolbar
		{
			get
			{
				return d_window != null ? d_window.ShowToolbar : d_showToolbar;
			}
			set
			{
				d_showToolbar = value;
			}
		}
		
		[XmlElement("show-pathbar")]
		public bool ShowPathbar
		{
			get
			{
				return d_window != null ? d_window.ShowPathbar : d_showPathbar;
			}
			set
			{
				d_showPathbar = value;
			}
		}
		
		[XmlElement("show-statusbar")]
		public bool ShowStatusbar
		{
			get
			{
				return d_window != null ? d_window.ShowStatusbar : d_showStatusbar;
			}
			set
			{
				d_showStatusbar = value;
			}
		}
		
		[XmlElement("show-simulate-buttons")]
		public bool ShowSimulateButtons
		{
			get
			{
				return d_window != null ? d_window.ShowStatusbar : d_showSimulateButtons;
			}
			set
			{
				d_showSimulateButtons = value;
			}
		}
	}
}
