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
		private static double Epsilon = 0.0000001;
		
		public event EventHandler Changed = delegate {};
		
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
		
		private void EmitChanged()
		{
			Changed(this, new EventArgs());
		}
		
		[XmlAttribute("x")]
		public double X
		{
			get { return d_x; }
			set
			{
				d_x = value;
				
				EmitChanged();
			}
		}
		
		[XmlAttribute("y")]
		public double Y
		{
			get { return d_y; }
			set
			{
				d_y = value;
				EmitChanged();
			}
		}
		
		[XmlAttribute("width")]
		public double Width
		{
			get { return d_width; }
			set
			{
				d_width = value;
				EmitChanged();
			}
		}
		
		[XmlAttribute("height")]
		public double Height
		{
			get { return d_height; }
			set
			{
				d_height = value;
				EmitChanged();
			}
		}
		
		public void Assign(double x, double y, double width, double height)
		{
			d_x = x;
			d_y = y;
			d_width = width;
			d_height = height;
			
			EmitChanged();
		}
		
		public void Offset(Point point)
		{
			Offset(point.X, point.Y);
		}
		
		public void Offset(double x, double y)
		{
			d_x += x;
			d_y += y;
			
			EmitChanged();
		}
		
		public void Move(double x, double y)
		{
			d_x = x;
			d_y = y;
			
			EmitChanged();
		}
		
		public void Move(Point point)
		{
			Move(point.X, point.Y);
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
			d_y *= scale;
			d_width *= scale;
			d_height *= scale;
			
			EmitChanged();
		}
		
		public void GrowBorder(double num)
		{
			d_x -= num;
			d_y -= num;
			d_width += num * 2;
			d_height += num * 2;
			
			EmitChanged();
		}
		
		public void Round()
		{
			d_x = System.Math.Round(d_x);
			d_y = System.Math.Round(d_y);
			d_width = System.Math.Round(d_width);
			d_height = System.Math.Round(d_height);
			
			EmitChanged();
		}
		
		public override string ToString()
		{
			return String.Format("[Allocation: x = {0}, y = {1}, width = {2}, height = {3}]", d_x, d_y, d_width, d_height);
		}
		
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			
			Allocation other = obj as Allocation;
			
			if (other == null)
			{
				return false;
			}
			
			return System.Math.Abs(d_x - other.X) < Epsilon &&
			       System.Math.Abs(d_y - other.Y) < Epsilon &&
			       System.Math.Abs(d_width - other.Width) < Epsilon &&
			       System.Math.Abs(d_height - other.Height) < Epsilon;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
