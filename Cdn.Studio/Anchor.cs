using System;
using Biorob.Math;

namespace Cdn.Studio
{
	public class Anchor
	{
		private Point d_location;
		private Wrappers.Edge d_edge;
		private bool d_isFrom;
		
		public Anchor(Wrappers.Edge link, Point location, bool isFrom)
		{
			d_edge = link;
			d_location = new Point(location);
			d_isFrom = isFrom;
		}
		
		public Wrappers.Wrapper Other
		{
			get
			{
				return d_isFrom ? d_edge.Output : d_edge.Input;
			}
		}
		
		public Wrappers.Edge Edge
		{
			get
			{
				return d_edge;
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
				return d_isFrom ? d_edge.Input : d_edge.Output;
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

