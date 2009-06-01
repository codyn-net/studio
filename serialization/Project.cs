using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cpg.Studio.Serialization
{
	[XmlType("project")]
	public class Project
	{
		Window d_window;
		Group d_root;

		public Project(Window window)
		{
			d_window = window;
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
				return d_window.Grid.GridSize;
			}
			set
			{
				// TODO
			}
		}
		
		[XmlElement("allocation")]
		public Allocation Allocation
		{
			get
			{
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
				// TODO
			}
		}
		
		[XmlElement("period"),
		 System.ComponentModel.DefaultValue("")]
		public string Period
		{
			get
			{
				return d_window.Period;
			}
			set
			{
				// TODO
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
	}
}
