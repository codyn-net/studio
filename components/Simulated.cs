using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Components
{
	public class Simulated : Components.Object, IDisposable
	{
		protected Cpg.Object d_object;
		protected List<Components.Link> d_links;

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
			d_links = new List<Link>();

			Object = obj;
		}
		
		public Simulated() : this(null)
		{
		}
		
		public void Dispose()
		{
			if (d_object != null)
			{
				d_object.RemoveNotification("id", NotifyIdHandler);

				d_object.Dispose();
				d_object = null;
			}
		}

		public override bool HasProperty(string name)
		{
			return (d_object != null && d_object.HasProperty(name)) || base.HasProperty(name);
		}
		
		public override object GetProperty(string name)
		{
			if (d_object == null)
				return base.GetProperty(name);
			
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
				Cpg.Property[] props = d_object != null ? d_object.Properties : new Cpg.Property[0];

				string[] ret = new string[props.Length + orig.Length];

				orig.CopyTo(ret, 0);
				
				for (int i = 0; i < props.Length; ++i)
					ret[i + orig.Length] = props[i].Name;
				
				return ret;
			}
		}
		
		public bool SimulatedProperty(string name)
		{
			return Object.Property(name) != null;
		}
		
		public virtual bool GetIntegrated(string name)
		{
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
				return prop.Integrated;
			else
				return false;
		}
		
		public virtual void SetIntegrated(string name, bool integrated)
		{
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
				prop.Integrated = integrated;
		}
		
		public virtual Cpg.PropertyHint GetHint(string name)
		{
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
			{
				return prop.Hint;
			}
			else
			{
				return Cpg.PropertyHint.None;
			}
		}
		
		public virtual void SetHint(string name, Cpg.PropertyHint hint)
		{
			Cpg.Property prop = d_object.Property(name);
			
			if (prop != null)
			{
				prop.Hint = hint;
			}
		}
		
		private void NotifyIdHandler(object source, GLib.NotifyArgs args)
		{
			if (base.Id != d_object.Id)
			{
				base.Id = d_object.Id;
			}
		}
		
		public virtual Cpg.Object Object
		{
			get
			{
				return d_object;
			}
			set
			{
				if (d_object != null)
				{
					d_object.RemoveNotification("id", NotifyIdHandler);
				}
				
				d_object = value;
				
				if (d_object != null)
				{
					d_object.AddNotification("id", NotifyIdHandler);
				}
			}
		}
		
		public List<Components.Link> Links
		{
			get
			{
				return d_links;
			}
		}
		
		public void Link(Components.Link link)
		{
			d_links.Add(link);
		}
		
		public void Unlink(Components.Link link)
		{
			d_links.Remove(link);
		}
		
		private string GetNamespace()
		{
			Components.Group group = Parent;
			List<string> ns = new List<string>();
			
			while (group != null && group.Parent != null)
			{
				ns.Insert(0, group.Id);
				group = group.Parent;
			}
			
			return String.Join(".", ns.ToArray());
		}

		[Property("id")]
		public override string Id
		{
			get
			{
				return d_object != null ? d_object.LocalId : base.Id;
			}
			
			set
			{
				if (d_object != null)
				{
					// Check namespace
					string ns = GetNamespace();
					string v = value;
					
					if (ns != String.Empty && !v.StartsWith(ns + "."))
						v = ns + "." + v;
					
					d_object.Id = v;
					base.Id = d_object.Id;
				}
				else
				{
					base.Id = value;
				}
			}
		}
		
		public string FullId
		{
			get
			{
				return d_object != null ? d_object.Id : base.Id;
			}
		}
		
		public override void Rename()
		{
			string ns = GetNamespace();
			string newid;
			
			if (ns != "")
			{
				newid = ns + "." + d_object.LocalId;
			}
			else
			{
				newid = d_object.LocalId;
			}
			
			if (d_object.Id != newid)
				d_object.Id = newid;
		}
		
		public override void DoRequestRedraw()
		{
			foreach (Components.Link link in d_links)
				link.DoRequestRedraw();
				
			base.DoRequestRedraw();
			
			foreach (Components.Link link in d_links)
				link.DoRequestRedraw();
		}
		
		public virtual bool CanIntegrate
		{
			get
			{
				return true;
			}
		}
	}
}
