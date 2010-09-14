using System;

namespace Cpg.Studio
{
	public class Anchor
	{
		private Point d_location;
		private Wrappers.Link d_link;
		private Wrappers.Wrapper d_object;
		
		public Anchor(Wrappers.Link link, Wrappers.Wrapper obj, Point location)
		{
			d_link = link;
			d_object = obj;
			d_location = new Point(location);
		}
		
		public Wrappers.Link Link
		{
			get
			{
				return d_link;
			}
			set
			{
				d_link = value;
			}
		}
		
		public Wrappers.Wrapper Object
		{
			get
			{
				return d_object;
			}
			set
			{
				d_object = value;
			}
		}
		
		public Point Location
		{
			get
			{
				return d_location;
			}
			set
			{
				d_location = value;
			}
		}
	}
}

