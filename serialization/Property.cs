using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("property")]
	public class Property
	{
		string d_name;
		string d_value;
		bool d_integrated;
		
		public Property(string name, string val, bool integrated)
		{
			d_name = name;
			d_value = val;
			d_integrated = integrated;
		}
		
		public Property()
		{
		}
		
		[XmlAttribute("name")]
		public string Name
		{
			get
			{
				return d_name;
			}
			set
			{
				d_name = value;
			}
		}
		
		[XmlAttribute("integrated"),
		 System.ComponentModel.DefaultValue(false)]
		public bool Integrated
		{
			get
			{
				return d_integrated;
			}
			set
			{
				d_integrated = value;
			}
		}
		
		[XmlText()]
		public string Value
		{
			get
			{
				return d_value;
			}
			set
			{
				d_value = value;
			}
		}
	}
}
