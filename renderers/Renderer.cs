using System;

namespace Cpg.Studio.Components.Renderers
{
	public class Renderer
	{
		protected Components.Group d_group;

		public Renderer(Components.Group obj)
		{
			d_group = obj;
		}
		
		public virtual void Draw(Cairo.Context graphics)
		{
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