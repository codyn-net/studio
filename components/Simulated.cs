using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Components
{
	public class Simulated : Components.Object
	{
		protected Cpg.Object d_object;

		public static Components.Simulated FromCpg(Cpg.Object obj)
		{
			Type type = obj.GetType();
			
			if (type == typeof(Cpg.Relay))
				return new Components.Relay(obj as Cpg.Relay);
			else if (type == typeof(Cpg.State))
				return new Components.State(obj as Cpg.State);
			else if (type == typeof(Cpg.Link))
				return new Components.Link(obj as Cpg.Link);
			else
				return null;
		}
		
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
		
		public virtual Cpg.Object Object
		{
			get
			{
				return d_object;
			}
			set
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
				DoRequestRedraw();
			}
		}
		
		public override string ToString()
		{
			return d_object.Id;
		}
	}
}
