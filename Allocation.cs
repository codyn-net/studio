using System;

namespace Cpg.Studio
{
	public class Allocation
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;
		
		public Allocation()
		{
			X = 0;
			Y = 0;
			Width = 1;
			Height = 1;
		}
		
		public Allocation(int x, int y, int width, int height)
		{
			Assign(x, y, width, height);
		}
		
		public void Assign(int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
	}
}
