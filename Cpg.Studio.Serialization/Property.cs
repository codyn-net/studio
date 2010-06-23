using System;
using System.Xml.Serialization;

namespace Cpg.Studio.Serialization
{
	[XmlType("property")]
	public class Property
	{
		Cpg.Property d_property;

		string d_name;
		string d_value;
		Cpg.PropertyFlags d_flags;
		
		public Property(Cpg.Property property)
		{
			d_property = property;
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
		
		[XmlAttribute("flags")]
		public string Flags
		{
			get
			{
				if (d_property != null)
				{
					if (!d_property.Data.Contains("CpgNetworkXmlPropertyFlagsAttribute"))
					{
						return "";
					}
					else
					{
						return Cpg.Property.FlagsToString(d_property.Flags);
					}
				}
				else
				{
					return Cpg.Property.FlagsToString(d_flags);
				}
			}
			set
			{
				if (d_property != null)
				{
					d_property.Flags = Cpg.Property.FlagsFromString(value);
				}
				else
				{
					d_flags = Cpg.Property.FlagsFromString(value);
				}
			}
		}
		
		private string HasFlag(Cpg.PropertyFlags flag)
		{
			Cpg.PropertyFlags ret;

			if (d_property != null)
			{
				ret = d_property.Flags & flag;
			}
			else
			{
				ret = d_flags & flag;
			}
			
			return (ret != Cpg.PropertyFlags.None) ? "yes" : null;
		}
		
		private void AddFlag(Cpg.PropertyFlags flag, string val)
		{
			if (String.IsNullOrEmpty(val))
			{
				if (d_property != null)
				{
					d_property.RemoveFlags(flag);
				}
				else
				{
					d_flags &= ~flag;
				}
			}
			else
			{
				if (d_property != null)
				{
					d_property.AddFlags(flag);
				}
				else
				{
					d_flags |= flag;
				}
			}
		}
		
		[XmlAttribute("in")]
		public string In
		{
			get
			{
				return HasFlag(Cpg.PropertyFlags.In);
			}
			set
			{
				AddFlag(Cpg.PropertyFlags.In, value);
			}
		}

		[XmlAttribute("out")]
		public string Out
		{
			get
			{
				return HasFlag(Cpg.PropertyFlags.Out);
			}
			set
			{
				AddFlag(Cpg.PropertyFlags.Out, value);
			}
		}
		
		[XmlAttribute("once")]
		public string Once
		{
			get
			{
				return HasFlag(Cpg.PropertyFlags.Once);
			}
			set
			{
				AddFlag(Cpg.PropertyFlags.Once, value);
			}
		}
		
		[XmlAttribute("integrated")]
		public string Integrated
		{
			get
			{
				return HasFlag(Cpg.PropertyFlags.Integrated);
			}
			set
			{
				AddFlag(Cpg.PropertyFlags.Integrated, value);
			}
		}
		
		[XmlText()]
		public string Value
		{
			get
			{
				return d_property == null ? d_value : d_property.Expression.AsString;
			}
			set
			{
				if (d_property == null)
				{
					d_value = value;
				}
				else
				{
					d_property.Expression.FromString = value;
				}
			}
		}
	}
}
