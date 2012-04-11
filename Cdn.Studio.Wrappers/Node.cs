using System;
using System.Collections.Generic;
using System.Reflection;
using CCdn = Cdn;

namespace Cdn.Studio.Wrappers
{
	public class Node : Wrappers.Object
	{
		public delegate void ChildHandler(Node source,Wrapper child);
		
		public event ChildHandler ChildAdded = delegate {};
		public event ChildHandler ChildRemoved = delegate {};

		private int d_x;
		private int d_y;
		private int d_zoom;
		
		public Node() : this(new Cdn.Node("node", null))
		{
		}
		
		protected Node(Cdn.Node obj) : base(obj)
		{
			d_x = 0;
			d_y = 0;
			d_zoom = Widgets.Grid.DefaultZoom;
			
			Renderer = new Renderers.Node(this);
		}
		
		public static implicit operator Cdn.Object(Node obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator Cdn.Node(Node obj)
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
		
		public bool VariableIsProxy(string name)
		{
			return WrappedObject.VariableIsProxy(name);
		}
		
		public new Cdn.Node WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.Node;
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
		
		public Cdn.Variable FindVariable(string name)
		{
			return WrappedObject.FindVariable(name);
		}

		public Cdn.Variable[] Actors
		{
			get
			{
				return WrappedObject.Actors;
			}
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
			return Array.IndexOf<Cdn.Object>(WrappedObject.Children, obj.WrappedObject);
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
		
		public Cdn.VariableInterface VariableInterface
		{
			get
			{
				return WrappedObject.VariableInterface;
			}
		}
		
		public IEnumerable<Wrappers.Function> Functions
		{
			get
			{
				foreach (Wrappers.Wrapper obj in Children)
				{
					Wrappers.Function func = obj as Wrappers.Function;
					
					if (func != null)
					{
						yield return func;
					}
				}
			}
		}
		
		public Wrappers.Function GetFunction(string name)
		{
			return GetChild(name) as Wrappers.Function;
		}
	}
}
