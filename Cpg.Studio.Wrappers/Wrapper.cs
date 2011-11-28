using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio.Wrappers
{
	public class Wrapper : Graphical, IDisposable
	{
		public delegate void PropertyHandler(Wrapper source, Cpg.Property property);

		public delegate void TemplateHandler(Wrapper source, Wrapper template);

		protected Cpg.Object d_object;
		protected List<Wrappers.Link> d_links;
		
		public event PropertyHandler PropertyAdded = delegate {};
		public event PropertyHandler PropertyRemoved = delegate {};
		public event PropertyHandler PropertyChanged = delegate {};
		public event TemplateHandler TemplateApplied = delegate {};
		public event TemplateHandler TemplateUnapplied = delegate {};
		
		public static string WrapperDataKey = "CpgStudioWrapperDataKey";
		private static Dictionary<Type, ConstructorInfo> s_typeMapping;
		protected const int RenderAnnotationAtsize = 16;
		
		private static ConstructorInfo WrapperConstructor(Type wrapperType, Type cpgType)
		{
			BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			
			return wrapperType.GetConstructor(binding, null, new Type[] {cpgType}, null);
		}
		
		private static Dictionary<string, Type> ScanWrappers()
		{
			Dictionary<string, Type > ret = new Dictionary<string, Type>();

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
			Dictionary<string, Type > wrapperTypes = ScanWrappers();
			
			Type cpgObjectType = typeof(Cpg.Object);
			
			foreach (Type type in cpgObjectType.Assembly.GetTypes())
			{
				if (type.IsSubclassOf(cpgObjectType))
				{
					// Then find the wrapper for it
					Type child = type;
					
					while (!s_typeMapping.ContainsKey(child))
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
			
			if (obj.SupportsLocation())
			{
				int x;
				int y;

				obj.GetLocation(out x, out y);
				
				Allocation.X = x;
				Allocation.Y = y;
			}
			
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
			Cpg.Property prop = new Cpg.Property(name, new Cpg.Expression(val), flags);
			
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
		
		private void NotifyAnnotationHandler(object source, GLib.NotifyArgs args)
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
			
			if (WrappedObject is Cpg.Annotatable)
			{
				WrappedObject.RemoveNotification("annotation", NotifyAnnotationHandler);
			}

			WrappedObject.PropertyAdded -= HandlePropertyAdded;
			WrappedObject.PropertyRemoved -= HandlePropertyRemoved;
			WrappedObject.PropertyChanged -= HandlePropertyChanged;
			WrappedObject.Copied -= HandleCopied;
			
			WrappedObject.TemplateApplied -= HandleTemplateApplied;
			WrappedObject.TemplateUnapplied -= HandleTemplateUnapplied;
			
			if (WrappedObject.SupportsLocation())
			{
				RemoveLocationNotification();
			}
		}
		
		private void AddLocationNotifification()
		{
			WrappedObject.AddNotification("x", OnLocationChanged);
			WrappedObject.AddNotification("y", OnLocationChanged);
		}
		
		private void RemoveLocationNotification()
		{
			WrappedObject.RemoveNotification("x", OnLocationChanged);
			WrappedObject.RemoveNotification("y", OnLocationChanged);
		}
		
		protected virtual void ConnectWrapped()
		{
			WrappedObject.AddNotification("id", NotifyIdHandler);
			
			if (WrappedObject is Cpg.Annotatable)
			{
				WrappedObject.AddNotification("annotation", NotifyAnnotationHandler);
			}
				
			WrappedObject.PropertyAdded += HandlePropertyAdded;
			WrappedObject.PropertyRemoved += HandlePropertyRemoved;
			WrappedObject.PropertyChanged += HandlePropertyChanged;
			WrappedObject.Copied += HandleCopied;
			WrappedObject.TemplateApplied += HandleTemplateApplied;
			WrappedObject.TemplateUnapplied += HandleTemplateUnapplied;
			
			if (WrappedObject.SupportsLocation())
			{
				AddLocationNotifification();
				
				Moved += delegate(object sender, EventArgs e) {
					int x;
					int y;

					WrappedObject.GetLocation(out x, out y);
					
					if ((int)Allocation.X != x || (int)Allocation.Y != y)
					{
						RemoveLocationNotification();
						WrappedObject.SetLocation((int)Allocation.X, (int)Allocation.Y);
						AddLocationNotifification();
					}
				};
			}
		}
		
		private void OnLocationChanged(object source, GLib.NotifyArgs args)
		{
			int x;
			int y;

			WrappedObject.GetLocation(out x, out y);
			
			Allocation.X = x;
			Allocation.Y = y;
		}

		private void HandleTemplateUnapplied(object o, TemplateUnappliedArgs args)
		{
			TemplateUnapplied(this, args.Templ);
		}

		private void HandleTemplateApplied(object o, TemplateAppliedArgs args)
		{
			TemplateApplied(this, args.Templ);
		}

		private void HandleCopied(object o, CopiedArgs args)
		{
			Wrapper wrapped = Wrap(args.Copy);
			
			wrapped.Allocation = Allocation.Copy();
			
			if (Renderer != null)
			{
				wrapped.Renderer = Renderer.Copy(wrapped);
			}
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
		
		public Wrappers.Wrapper[] TemplateAppliesTo
		{
			get
			{
				return Wrap(d_object.TemplateAppliesTo);
			}
		}
		
		public Wrappers.Wrapper Copy()
		{
			return Wrappers.Wrapper.Wrap(WrappedObject.Copy());
		}
		
		public Wrappers.Wrapper CopyAsTemplate()
		{
			return Wrappers.Wrapper.Wrap(WrappedObject.CopyAsTemplate());
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
		
		public bool VerifyRemoveProperty(string prop)
		{
			return WrappedObject.VerifyRemoveProperty(prop);
		}
		
		public Wrappers.Wrapper GetPropertyTemplate(Cpg.Property prop, bool matchFull)
		{
			return Wrappers.Wrapper.Wrap(WrappedObject.GetPropertyTemplate(prop, matchFull));
		}
		
		public virtual void Removed()
		{
		}
		
		public string FullId
		{
			get
			{
				return WrappedObject.FullIdForDisplay;
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
		
		public void ApplyTemplate(Wrappers.Wrapper template)
		{
			WrappedObject.ApplyTemplate(template.WrappedObject);
		}
		
		public void UnapplyTemplate(Wrappers.Wrapper template)
		{
			WrappedObject.UnapplyTemplate(template.WrappedObject);
		}
		
		protected virtual void SizeOnCanvas(Cairo.Context context, ref double width, ref double height)
		{
			width *= context.Matrix.Xx;
			height *= context.Matrix.Yy;
		}
		
		public virtual void AnnotationHotspot(Cairo.Context context, double width, double height, int size, out double x, out double y)
		{
			x = 2; //width - size - 2;
			y = 2;
		}
		
		public virtual bool CanDrawAnnotation(Cairo.Context context)
		{
			double w = Allocation.Width;
			double h = Allocation.Height;
			
			SizeOnCanvas(context, ref w, ref h);
			
			return w > 2 * RenderAnnotationAtsize && h > 2 * RenderAnnotationAtsize;
		}
		
		protected virtual void DrawAnnotation(Cairo.Context context, Cpg.Annotatable annotatable)
		{
			if (!CanDrawAnnotation(context))
			{
				return;
			}

			double w = Allocation.Width;
			double h = Allocation.Height;
			
			SizeOnCanvas(context, ref w, ref h);
			
			int size = RenderAnnotationAtsize;
			double x;
			double y;

			Cairo.Surface surf = Stock.Surface(context, Gtk.Stock.Info, size);
			
			context.Save();
			
			AnnotationHotspot(context, w, h, size, out x, out y);
			
			context.Scale(1 / context.Matrix.Xx, 1 / context.Matrix.Yy);

			context.SetSourceSurface(surf, (int)x, (int)y);
			context.Paint();
			
			context.Restore();
		}
		
		public override void Draw(Cairo.Context context)
		{
			base.Draw(context);
			
			Cpg.Annotatable annotatable = WrappedObject as Cpg.Annotatable;
			
			if (annotatable != null && !String.IsNullOrEmpty(annotatable.Annotation))
			{
				DrawAnnotation(context, annotatable);
			}
		}
	}
}
