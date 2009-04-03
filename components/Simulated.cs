using System;
using System.Collections.Generic;

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
			return d_object.HasProperty(name);
		}
		
		public override object GetProperty(string name)
		{
			if (d_object == null)
				return null;
			
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
				return prop.ValueExpression.AsString;
			else
				return null;
		}
		
		protected override void SetPropertyReal(string name, object val)
		{
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
			{
				if (val == null)
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
				Property[] props = d_object.Properties;
				string[] ret = new string[props.Length];
				
				for (int i = 0; i < props.Length; ++i)
					ret[i] = props[i].Name;
				
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
	}
}
