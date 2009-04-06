using System;
using System.Drawing;

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
		
		public float X
		{
			get { return d_rectangle.X; }
			set { d_rectangle.X = value; }
		}
		
		public float Y
		{
			get { return d_rectangle.Y; }
			set { d_rectangle.Y = value; }
		}
		
		public float Width
		{
			get { return d_rectangle.Width; }
			set { d_rectangle.Width = value; }
		}
		
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
