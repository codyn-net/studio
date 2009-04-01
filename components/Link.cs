using System;

namespace Cpg.Studio.Components
{
	public class Link : Simulated
	{
		Components.Object d_from;
		Components.Object d_to;
		int d_offset;

		public Link(Cpg.Link obj) : base(obj)
		{
		}
		
		public Link() : this(null)
		{
		}
		
		public Components.Object[] Objects
		{
			get
			{
				if (d_object != null)
					return new Components.Object[] {d_from, d_to};
				else
					return new Components.Object[] {};
			}
		}
		
		public int Offset
		{
			get
			{
				return d_offset;
			}
			set
			{
				d_offset = value;
			}
		}
		
		public bool Empty()
		{
			return d_object == null;
		}
		
		public bool SameObjects(Components.Link other)
		{
			return other.From == d_from && other.To == d_to;
		}
		
		public Components.Object From
		{
			get
			{
				return d_from;
			}
		}
		
		public Components.Object To
		{
			get
			{
				return d_to;
			}
		}
		
		public bool HitTest(System.Drawing.Rectangle rect, int gridSize)
		{
			return false;
		}
	}
}
