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
		
		private static Dictionary<Type, ConstructorInfo> s_typeMapping;
		
		private static ConstructorInfo WrapperConstructor(Type wrapperType, Type cpgType)
		{
			BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			
			return wrapperType.GetConstructor(binding, null, new Type[] {cpgType}, null);
		}
		
		private static Dictionary<string, Type> ScanWrappers()
		{
			Dictionary<string, Type> ret = new Dictionary<string, Type>();

			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (type.IsSubclassOf(typeof(Wrappers.Wrapper)))
				{
					ret[type.Name] = type;
				}
			}

			return ret;
		}
		
		static Wrapper()
		{
			s_typeMapping = new Dictionary<Type, ConstructorInfo>();
			Dictionary<string, Type> wrapperTypes = ScanWrappers();
			
			Type cpgObjectType = typeof(Cpg.Object);
			
			foreach (Type type in cpgObjectType.Assembly.GetTypes())
			{
				if (type.IsSubclassOf(cpgObjectType))
				{
					// Then find the wrapper for it
					Type child = type;
					
					while (child != cpgObjectType && !s_typeMapping.ContainsKey(child))
					{
						// Find corresponding wrapper
						if (wrapperTypes.ContainsKey(child.Name))
						{
							Type wrapperType = wrapperTypes[child.Name];
							ConstructorInfo info = WrapperConstructor(wrapperType, child);
							
							if (info != null)
							{
								s_typeMapping[child] = info;
								break;
							}
							else
							{
								Console.Error.WriteLine("Could not find constructor for wrapper `{0}' => `{1}'", wrapperType, child);
							}
						}
						
						child = child.BaseType;
					}
				}
			}
		}

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
			
			Type cpgType = obj.GetType();
			
			if (!s_typeMapping.ContainsKey(cpgType))
			{
				Console.Error.WriteLine("Could not find wrapper for type `{0}'", cpgType);
				return null;
			}	
			
			return (Wrapper)s_typeMapping[cpgType].Invoke(new object[] {obj});			
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
		
		protected Wrapper(Cpg.Object obj) : base()
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
		
		public bool AddProperty(Cpg.Property property)
		{
			return d_object.AddProperty(property);
		}
		
		public Cpg.Property AddProperty(string name, string val, Cpg.PropertyFlags flags)
		{
			Cpg.Property prop = new Cpg.Property(name, val, flags);
			
			if (AddProperty(prop))
			{
				return prop;
			}
			else
			{
				return null;
			}
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
		
		public string FullId
		{
			get
			{
				if (Parent != null && Parent.Parent != null)
				{
					return Parent.FullId + "." + Id;
				}
				else if (Parent == null)
				{
					return "";
				}
				else
				{
					return Id;
				}
			}
		}
		
		public Wrappers.Group TopParent
		{
			get
			{
				if (Parent == null)
				{
					return this as Wrappers.Group;
				}
				else if (Parent.Parent == null)
				{
					return Parent;
				}
				else
				{
					return Parent.TopParent;
				}
			}
		}
	}
}
