using System;
using System.Collections.Generic;
using System.Reflection;
using CCpg = Cpg;

namespace Cpg.Studio.Wrappers
{
	public class Group : Wrappers.Object
	{
		public delegate void ChildHandler(Group source, Wrapper child);
		
		public event ChildHandler ChildAdded = delegate {};
		public event ChildHandler ChildRemoved = delegate {};

		private int d_x;
		private int d_y;
		private int d_zoom;
		
		public Group() : this(new Cpg.Group("group", null))
		{
		}
		
		protected Group(Cpg.Group obj) : base(obj)
		{
			d_x = 0;
			d_y = 0;
			d_zoom = Widgets.Grid.DefaultZoom;
			
			Renderer = new Renderers.Group(this);
		}
		
		public static implicit operator Cpg.Object(Group obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator Cpg.Group(Group obj)
		{
			return obj.WrappedObject;
		}
		
		protected override void ConnectWrapped()
		{
			base.ConnectWrapped();

			WrappedObject.ChildAdded += HandleChildAdded;
			WrappedObject.ChildRemoved += HandleChildRemoved;
		}
		
		protected override void DisconnectWrapped()
		{
			base.DisconnectWrapped();

			WrappedObject.ChildAdded -= HandleChildAdded;
			WrappedObject.ChildRemoved -= HandleChildRemoved;
		}

		private void HandleChildAdded(object o, ChildAddedArgs args)
		{
			ChildAdded(this, args.Object);
		}
		
		private void HandleChildRemoved(object o, ChildRemovedArgs args)
		{
			ChildRemoved(this, args.Object);
		}
		
		public bool PropertyIsProxy(string name)
		{
			return WrappedObject.PropertyIsProxy(name);
		}
		
		public new Cpg.Group WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Group;
			}
		}
		
		public int X
		{
			get { return d_x; }
			set { d_x = value; }
		}
		
		public int Y
		{
			get { return d_y; }
			set { d_y = value; }
		}
		
		public int Zoom
		{
			get { return d_zoom; }
			set { d_zoom = value; }
		}
		
		public bool Add(Wrapper wrapped)
		{
			return WrappedObject.Add(wrapped.WrappedObject);
		}
		
		public bool Remove(Wrapper wrapped)
		{
			return WrappedObject.Remove(wrapped.WrappedObject);
		}
		
		public Wrapper GetChild(string name)
		{
			return WrappedObject.GetChild(name);
		}
		
		public Wrapper FindObject(string name)
		{
			return WrappedObject.FindObject(name);
		}
		
		public Cpg.Property FindProperty(string name)
		{
			return WrappedObject.FindProperty(name);
		}
		
		public Wrapper[] Children
		{
			get
			{
				return Wrap(WrappedObject.Children);
			}
		}
		
		public int Length
		{
			get
			{
				return WrappedObject.Children.Length;
			}
		}
		
		public bool Contains(Wrapper obj)
		{
			return IndexOf(obj) != -1;
		}
		
		public int IndexOf(Wrapper obj)
		{
			return Array.IndexOf<Cpg.Object>(WrappedObject.Children, obj.WrappedObject);
		}
		
		public Wrapper Proxy
		{
			get
			{
				return WrappedObject.Proxy;
			}
		}
		
		public bool SetProxy(Wrapper val)
		{
			return WrappedObject.SetProxy(val);
		}
	}
}
