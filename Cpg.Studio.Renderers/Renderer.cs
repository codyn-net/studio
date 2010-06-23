using System;
using System.Reflection;

namespace Cpg.Studio.Wrappers.Renderers
{
	public class Renderer
	{
		protected Wrappers.Wrapper d_object;

		public Renderer(Wrappers.Wrapper obj)
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
				
				try
				{
					ret = new Gdk.Pixbuf(data, Gdk.Colorspace.Rgb, true, 8, size, size, surface.Stride);
				}
				catch
				{
					// Some stupid bug in older mono, fallback strategy
					string filename = System.IO.Path.GetTempFileName();
					
					if (System.IO.File.Exists(filename))
						System.IO.File.Delete(filename);
						
					surface.WriteToPng(filename);
					ret = new Gdk.Pixbuf(filename);
					System.IO.File.Delete(filename);
				}
			}	
			
			return ret;
		}
		
		public static string GetName(Type type)
		{
			object[] attributes = type.GetCustomAttributes(typeof(Wrappers.Renderers.NameAttribute), true);
			
			if (attributes.Length != 0)
			{
				return (attributes[0] as Wrappers.Renderers.NameAttribute).Name;
			}
			else
			{
				return "None";	
			}
		}
		
		public static Type FindByName(string name, Type subclassof)
		{
			Assembly asm = Assembly.GetEntryAssembly();
			
			foreach (Type type in asm.GetTypes())
			{
				if (subclassof != null && !type.IsSubclassOf(subclassof))
					continue;

				if (GetName(type) == name)
					return type;
			}
			
			return null;
		}
		
		public static Type FindByName(string name)
		{
			return FindByName(name, null);
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