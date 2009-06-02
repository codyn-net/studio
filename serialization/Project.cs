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

		public Project(Window window)
		{
			d_window = window;
			
			d_zoom = 50;
			d_period = "";
			d_panePosition = 200;
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
		
		[XmlElement("panePosition")]
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
	}
}
