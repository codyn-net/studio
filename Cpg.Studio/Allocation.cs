using System;
using System.Drawing;
using System.Xml.Serialization;

namespace Cpg.Studio
{
	public class Allocation
	{
		private RectangleF d_rectangle;
		
		public Allocation(RectangleF rect)
		{
			d_rectangle = rect;
		}

		public Allocation(double x, double y, double width, double height) : this(new RectangleF((float)x, (float)y, (float)width, (float)height))
		{
		}
		
		public Allocation() : this(0, 0, 1, 1)
		{
		}
		
		[XmlAttribute("x")]
		public float X
		{
			get { return d_rectangle.X; }
			set { d_rectangle.X = value; }
		}
		
		[XmlAttribute("y")]
		public float Y
		{
			get { return d_rectangle.Y; }
			set { d_rectangle.Y = value; }
		}
		
		[XmlAttribute("width")]
		public float Width
		{
			get { return d_rectangle.Width; }
			set { d_rectangle.Width = value; }
		}
		
		[XmlAttribute("height")]
		public float Height
		{
			get { return d_rectangle.Height; }
			set { d_rectangle.Height = value; }
		}
		
		public void Assign(double x, double y, double width, double height)
		{
			d_rectangle = new RectangleF((float)x, (float)y, (float)width, (float)height);
		}
		
		public void Offset(double x, double y)
		{
			d_rectangle.Offset((float)x, (float)y);
		}
		
		public void Move(double x, double y)
		{
			d_rectangle.X = (float)x;
			d_rectangle.Y = (float)y;
		}
		
		public Allocation FromRegion()
		{
			Allocation rect = new Allocation(this);

			if (rect.Width < rect.X)
			{
				float tmp = rect.Width;
				rect.Width = rect.X;
				rect.X = tmp;
			}
			
			if (rect.Height < rect.Y)
			{
				float tmp = rect.Height;
				rect.Height = rect.Y;
				rect.Y = tmp;
			}
			
			return new Allocation(rect.X, rect.Y, rect.Width - rect.X, rect.Height - rect.Y);
		}

		public bool Intersects(Allocation other)
		{
			return d_rectangle.IntersectsWith(other.d_rectangle);
		}
		
		public void Scale(float scale)
		{
			d_rectangle.X *= scale;
			d_rectangle.Y *= scale;
			d_rectangle.Width *= scale;
			d_rectangle.Height *= scale;
		}
		
		public void GrowBorder(float num)
		{
			d_rectangle.X -= num;
			d_rectangle.Y -= num;
			d_rectangle.Width += num * 2;
			d_rectangle.Height += num * 2;
		}
		
		public void Round()
		{
			Math.Round(d_rectangle.X);
			Math.Round(d_rectangle.Y);
			Math.Round(d_rectangle.Width);
			Math.Round(d_rectangle.Height);
		}
		
		public override string ToString()
		{
			return d_rectangle.ToString();
		}
		
		public static implicit operator RectangleF(Allocation alloc)
		{
			return alloc.d_rectangle;
		}
		
		public static implicit operator Rectangle(Allocation alloc)
		{
			return new Rectangle((int)alloc.X, (int)alloc.Y, (int)alloc.Width, (int)alloc.Height);
		}
	}
}
