using System;

namespace Cpg.Studio
{
	public class Anchor
	{
		private Point d_location;
		private Wrappers.Link d_link;
		private bool d_isFrom;
		
		public Anchor(Wrappers.Link link, Point location, bool isFrom)
		{
			d_link = link;
			d_location = new Point(location);
			d_isFrom = isFrom;
		}
		
		public Wrappers.Wrapper Other
		{
			get
			{
				return d_isFrom ? d_link.To : d_link.From;
			}
		}
		
		public Wrappers.Link Link
		{
			get
			{
				return d_link;
			}
		}
		
		public bool IsFrom
		{
			get
			{
				return d_isFrom;
			}
		}
		
		public Wrappers.Wrapper Object
		{
			get
			{
				return d_isFrom ? d_link.From : d_link.To;
			}
		}
		
		public Point Location
		{
			get
			{
				return d_location;
			}
		}
	}
}

