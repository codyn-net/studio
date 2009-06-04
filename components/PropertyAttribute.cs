using System;

namespace Cpg.Studio.Components
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
	public class PropertyAttribute : System.Attribute
	{
		private string d_name;
		private bool d_readonly;
		private bool d_invisible;
		
		public PropertyAttribute(string name, bool readon, bool invisible)
		{
			d_name = name;
			d_readonly = readon;
			d_invisible = invisible;
		}
		
		public PropertyAttribute(string name, bool readon) : this(name, false, false)
		{
		}
		
		public PropertyAttribute(string name) : this(name, false)
		{
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public bool ReadOnly
		{
			get
			{
				return d_readonly;
			}
		}
		
		public bool Invisible
		{
			get
			{
				return d_invisible;
			}
		}
	}
}
