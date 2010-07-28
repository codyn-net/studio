using System;
using System.Xml.Serialization;

namespace Cpg.Studio
{
	public class Allocation
	{
		private double d_x;
		private double d_y;
		private double d_width;
		private double d_height;
		
		public Allocation(double x, double y, double width, double height)
		{
			d_x = x;
			d_y = y;
			d_width = width;
			d_height = height;
		}
		
		public Allocation() : this(0, 0, 1, 1)
		{
		}
		
		public Allocation Copy()
		{
			return new Allocation(d_x, d_y, d_width, d_height);
		}
		
		[XmlAttribute("x")]
		public double X
		{
			get { return d_x; }
			set { d_x = value; }
		}
		
		[XmlAttribute("y")]
		public double Y
		{
			get { return d_y; }
			set { d_y = value; }
		}
		
		[XmlAttribute("width")]
		public double Width
		{
			get { return d_width; }
			set { d_width = value; }
		}
		
		[XmlAttribute("height")]
		public double Height
		{
			get { return d_height; }
			set { d_height = value; }
		}
		
		public void Assign(double x, double y, double width, double height)
		{
			d_x = x;
			d_y = y;
			d_width = width;
			d_height = height;
		}
		
		public void Offset(double x, double y)
		{
			d_x += x;
			d_y += y;
		}
		
		public void Move(double x, double y)
		{
			d_x = x;
			d_y = y;
		}
		
		public Allocation FromRegion()
		{
			Allocation rect = Copy();

			if (rect.Width < rect.X)
			{
				double tmp = rect.Width;

				rect.Width = rect.X;
				rect.X = tmp;
			}
			
			if (rect.Height < rect.Y)
			{
				double tmp = rect.Height;

				rect.Height = rect.Y;
				rect.Y = tmp;
			}
			
			return new Allocation(rect.X, rect.Y, rect.Width - rect.X, rect.Height - rect.Y);
		}

		public bool Intersects(Allocation other)
		{
			return d_y + d_height >= other.Y &&
			       d_y <= other.Y + other.Height &&
			       d_x + d_width >= other.X &&
			       d_x <= other.X + other.Width;
		}
		
		public void Scale(double scale)
		{
			d_x *= scale;
			d_x *= scale;
			d_width *= scale;
			d_height *= scale;
		}
		
		public void GrowBorder(double num)
		{
			d_x -= num;
			d_y -= num;
			d_width += num * 2;
			d_height += num * 2;
		}
		
		public void Round()
		{
			d_x = Math.Round(d_x);
			d_y = Math.Round(d_y);
			d_width = Math.Round(d_width);
			d_height = Math.Round(d_height);
		}
		
		public override string ToString()
		{
			return String.Format("[Allocation: x = {0}, y = {1}, width = {2}, height = {3}]", d_x, d_y, d_width, d_height);
		}
	}
}
