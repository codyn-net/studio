using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Wrappers
{
	public class Wrapper : Graphical, IDisposable
	{
		public delegate void PropertyHandler(Wrapper source, Cpg.Property property);

		protected Cpg.Object d_object;
		protected List<Wrappers.Link> d_links;
		
		public event PropertyHandler PropertyAdded = delegate {};
		public event PropertyHandler PropertyRemoved = delegate {}; 
		public event PropertyHandler PropertyChanged = delegate {};
		
		public static string WrapperDataKey = "CpgStudioWrapperDataKey";

		public static Wrapper Wrap(Cpg.Object obj)
		{
			if (obj == null)
			{
				return null;
			}

			if (obj.Data.ContainsKey(WrapperDataKey))
			{
				return obj.Data[WrapperDataKey] as Wrapper;
			}

			Type type = obj.GetType();
			Wrapper ret;

			if (type == typeof(Cpg.Network))
			{
				ret = new Wrappers.Network(obj as Cpg.Network);
			}			
			if (type == typeof(Cpg.Group))
			{
				ret = new Wrappers.Group(obj as Cpg.Group);
			}
			else if (type == typeof(Cpg.State))
			{
				ret = new Wrappers.State(obj as Cpg.State);
			}
			else if (type == typeof(Cpg.Link))
			{
				ret = new Wrappers.Link(obj as Cpg.Link);
			}
			else
			{
				return null;
			}
			
			return ret;
		}
		
		public static Wrapper[] Wrap(Cpg.Object[] objs)
		{
			if (objs == null)
			{
				return new Wrapper[] {};
			}

			Wrapper[] ret = new Wrapper[objs.Length];
			
			for (int i = 0; i < objs.Length; ++i)
			{
				ret[i] = Wrap(objs[i]);
			}
			
			return ret;
		}
			
		public static implicit operator Cpg.Object(Wrapper obj)
		{
			if (obj == null)
			{
				return null;
			}

			return obj.WrappedObject;
		}
		
		public static implicit operator Wrapper(Cpg.Object obj)
		{
			if (obj == null)
			{
				return null;
			}

			return Wrap(obj);
		}
		
		public Wrapper(Cpg.Object obj) : base()
		{
			d_links = new List<Link>();

			d_object = obj;
			
			ConnectWrapped();

			obj.Data[WrapperDataKey] = this;
		}
		
		public Wrapper() : this(null)
		{
		}
		
		public void Dispose()
		{
			if (d_object != null)
			{
				DisconnectWrapped();

				d_object.Dispose();
				d_object = null;
			}
		}
		
		public Cpg.Property this[string name]
		{
			get
			{
				return Property(name);
			}
		}
		
		public Cpg.Property Property(string name)
		{
			return d_object.Property(name);
		}
		
		public Cpg.Property[] Actors
		{
			get
			{
				return d_object.Actors;
			}
		}
		
		public Wrapper[] AppliedTemplates
		{
			get
			{
				return Wrap(d_object.AppliedTemplates);
			}
		}
		
		public bool HasProperty(string name)
		{
			return d_object.HasProperty(name);
		}
		
		public Cpg.Property AddProperty(string name, string val, Cpg.PropertyFlags flags)
		{
			return d_object.AddProperty(name, val, flags);
		}
		
		public Cpg.Property AddProperty(string name, string val)
		{
			return AddProperty(name, val, Cpg.PropertyFlags.None);
		}
		
		public bool RemoveProperty(string name)
		{
			return d_object.RemoveProperty(name);
		}
		
		public bool IsCompiled
		{
			get
			{
				return d_object.IsCompiled;
			}
		}
		
		public Group Parent
		{
			get
			{
				return Wrap(d_object.Parent) as Group;
			}
		}
		
		public Cpg.Property[] Properties
		{
			get
			{
				return d_object.Properties;
			}
		}
		
		public void Clear()
		{
			d_object.Clear();
		}
		
		public void Taint()
		{
			d_object.Taint();
		}
		
		private void NotifyIdHandler(object source, GLib.NotifyArgs args)
		{
			DoRequestRedraw();
		}
		
		private void HandlePropertyAdded(object o, Cpg.PropertyAddedArgs args)
		{
			PropertyAdded(this, args.Property);
		}
		
		private void HandlePropertyRemoved(object o, Cpg.PropertyRemovedArgs args)
		{
			PropertyRemoved(this, args.Property);
		}
		
		private void HandlePropertyChanged(object o, Cpg.PropertyChangedArgs args)
		{
			PropertyChanged(this, args.Property);
		}
		
		protected virtual void DisconnectWrapped()
		{
			WrappedObject.RemoveNotification("id", NotifyIdHandler);

			WrappedObject.PropertyAdded -= HandlePropertyAdded;
			WrappedObject.PropertyRemoved -= HandlePropertyRemoved;
			WrappedObject.PropertyChanged -= HandlePropertyChanged;
			WrappedObject.Copied -= HandleCopied;
		}
		
		protected virtual void ConnectWrapped()
		{
			WrappedObject.AddNotification("id", NotifyIdHandler);
				
			WrappedObject.PropertyAdded += HandlePropertyAdded;
			WrappedObject.PropertyRemoved += HandlePropertyRemoved;
			WrappedObject.PropertyChanged += HandlePropertyChanged;
			WrappedObject.Copied += HandleCopied;
		}

		private void HandleCopied(object o, CopiedArgs args)
		{
			Wrapper wrapped = Wrap(args.Copy);
			
			wrapped.Allocation = Allocation.Copy();
			wrapped.Renderer = Renderer;
		}
		
		public virtual Cpg.Object WrappedObject
		{
			get
			{
				return d_object;
			}
		}
		
		public bool Compile()
		{
			return Compile(null);
		}
		
		public bool Compile(Cpg.CompileContext context)
		{
			return Compile(context, null);
		}
		
		public bool Compile(Cpg.CompileContext context, Cpg.CompileError error)
		{
			return WrappedObject.Compile(context, error);
		}
		
		public void Reset()
		{
			WrappedObject.Reset();
		}
	
		public List<Wrappers.Link> Links
		{
			get
			{
				return d_links;
			}
		}
		
		public void Link(Wrappers.Link link)
		{
			if (!d_links.Contains(link))
			{
				d_links.Add(link);
			}
		}
		
		public void Unlink(Wrappers.Link link)
		{
			d_links.Remove(link);
		}
		
		public Wrappers.Wrapper Copy()
		{
			return Wrappers.Wrapper.Wrap(WrappedObject.Copy());
		}

		public override string Id
		{
			get { return d_object.Id; }
			set { d_object.Id = value; }
		}
		
		public override void DoRequestRedraw()
		{
			foreach (Wrappers.Link link in d_links)
			{
				link.DoRequestRedraw();
			}
				
			base.DoRequestRedraw();
			
			foreach (Wrappers.Link link in d_links)
			{
				link.DoRequestRedraw();
			}
		}
		
		public virtual void Removed()
		{
		}
	}
}
