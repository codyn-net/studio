using System;

namespace Cpg.Studio.Components.Renderers
{
	public class Renderer
	{
		protected Components.Object d_object;

		public Renderer(Components.Object obj)
		{
			d_object = obj;
		}
		
		public Renderer() : this(null)
		{
		}
		
		public virtual void Draw(Cairo.Context graphics)
		{
		}
		
		private byte[] ConvertColorSpace(byte[] data)
		{
			byte[] ret = new byte[data.Length];
			
			for (int i = 0; i < data.Length; i += 4)
			{
				ret[i] = (byte)(data[i + 2] / (data[i + 3] / 255.0));
				ret[i + 1] = (byte)(data[i + 1] / (data[i + 3] / 255.0));
				ret[i + 2] = (byte)(data[i + 0] / (data[i + 3] / 255.0));
				ret[i + 3] = data[i + 3];
			}
			
			return ret;
		}
		
		public Gdk.Pixbuf Icon(int size)
		{
			Cairo.ImageSurface surface = new Cairo.ImageSurface(Cairo.Format.Argb32, size, size);
			Gdk.Pixbuf ret;
			
			using (Cairo.Context ct = new Cairo.Context(surface))
			{
				ct.Rectangle(0, 0, size, size);
				ct.Clip();
				
				ct.Translate(0, 0);
				ct.Scale(size, size);
				
				ct.LineWidth = 1.0 / size;
				
				Draw(ct);
				
				byte[] data = ConvertColorSpace(surface.Data);
				ret = new Gdk.Pixbuf(data, Gdk.Colorspace.Rgb, true, 8, size, size, surface.Stride);
				
				//ret.Save("/home/jesse/Desktop/" + GetType().Name + size + ".png", "png");
			}			
			
			return ret;
		}
	}
	
	
	[AttributeUsage(AttributeTargets.Class)]
	public class NameAttribute : Attribute
	{
		private string d_name;
		
		public NameAttribute(string name)
		{
			d_name = name;
		}
		
		public string Name
		{
			get
			{
				return d_name;	
			}
		}
	}
}