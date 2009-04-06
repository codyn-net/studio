using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Components
{
	public class Simulated : Components.Object
	{
		protected Cpg.Object d_object;

		public Simulated(Cpg.Object obj) : base()
		{
			d_object = obj;
		}
		
		public Simulated() : this(null)
		{
		}
		
		public override bool HasProperty(string name)
		{
			return d_object.HasProperty(name) || base.HasProperty(name);
		}
		
		public override object GetProperty(string name)
		{
			if (d_object == null)
				return null;
			
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
				return prop.ValueExpression.AsString;
			else
				return base.GetProperty(name);
		}
		
		protected override void SetPropertyReal(string name, object val)
		{
			PropertyInfo info;
			PropertyAttribute attr = FindPropertyAttribute(name, out info);
			
			if (attr != null)
			{
				info.GetSetMethod().Invoke(this, new object[] {val == null ? "" : val});
				return;
			}
			
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
			{
				if (val != null)
					prop.SetValueExpression((string)val);
				else
					d_object.RemoveProperty(name);
			}
			else if (val != null)
			{
				d_object.AddProperty(name, (string)val, false);
			}
		}
		
		public override string[] Properties
		{
			get
			{
				string[] orig = base.Properties;
				Property[] props = d_object.Properties;

				string[] ret = new string[props.Length + orig.Length];

				orig.CopyTo(ret, 0);
				
				for (int i = 0; i < props.Length; ++i)
					ret[i + orig.Length] = props[i].Name;
				
				return ret;
			}
		}
		
		public bool GetIntegrated(string name)
		{
			Property prop = d_object.Property(name);
			
			if (prop != null)
				return prop.Integrated;
			else
				return false;
		}
		
		public void SetIntegrated(string name, bool integrated)
		{
			Property prop = d_object.Property(name);
			
			if (prop != null)
				prop.Integrated = integrated;
		}
		
		public Cpg.Object Object
		{
			get
			{
				return d_object;
			}
			protected set
			{
				d_object = value;
			}
		}
		
		[Property("id")]
		public string Id
		{
			get
			{
				return d_object.Id;
			}
			
			set
			{
				d_object.Id = value;
				QueueDraw();
			}
		}
		
		public override string ToString()
		{
			return d_object.Id;
		}
	}
}
