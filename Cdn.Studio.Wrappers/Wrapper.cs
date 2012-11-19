using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cdn.Studio.Wrappers
{
	public class Wrapper : Graphical, IDisposable
	{
		public delegate void VariableHandler(Wrapper source,Cdn.Variable variable);

		public delegate void TemplateHandler(Wrapper source,Wrapper template);

		protected Cdn.Object d_object;
		protected List<Wrappers.Edge> d_links;
		protected AnnotationInfo d_annotationInfo;
		protected List<Cdn.Object> d_annotationRelative;
		
		public event VariableHandler VariableAdded = delegate {};
		public event VariableHandler VariableRemoved = delegate {};
		public event VariableHandler VariableChanged = delegate {};
		public event TemplateHandler TemplateApplied = delegate {};
		public event TemplateHandler TemplateUnapplied = delegate {};
		
		public static string WrapperDataKey = "CdnStudioWrapperDataKey";
		private static Dictionary<Type, ConstructorInfo> s_typeMapping;
		protected const int RenderAnnotationAtsize = 16;
		
		private static ConstructorInfo WrapperConstructor(Type wrapperType, Type cdnType)
		{
			BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			
			return wrapperType.GetConstructor(binding, null, new Type[] {cdnType}, null);
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
			
			Type cdnObjectType = typeof(Cdn.Object);
			
			foreach (Type type in cdnObjectType.Assembly.GetTypes())
			{
				if (type.IsSubclassOf(cdnObjectType))
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
								s_typeMapping[type] = info;
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

		public static Wrapper Wrap(Cdn.Object obj)
		{
			if (obj == null)
			{
				return null;
			}

			if (obj.Data.ContainsKey(WrapperDataKey))
			{
				return obj.Data[WrapperDataKey] as Wrapper;
			}
			
			Type cdnType = obj.GetType();
			
			if (!s_typeMapping.ContainsKey(cdnType))
			{
				Console.Error.WriteLine("Could not find wrapper for type `{0}'", cdnType);
				return null;
			}	
			
			return (Wrapper)s_typeMapping[cdnType].Invoke(new object[] {obj});			
		}
		
		public static Wrapper[] Wrap(Cdn.Object[] objs)
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
			
		public static implicit operator Cdn.Object(Wrapper obj)
		{
			if (obj == null)
			{
				return null;
			}

			return obj.WrappedObject;
		}
		
		public static implicit operator Wrapper(Cdn.Object obj)
		{
			if (obj == null)
			{
				return null;
			}

			return Wrap(obj);
		}
		
		protected Wrapper(Cdn.Object obj) : base()
		{
			d_links = new List<Edge>();

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

		protected void SetWrappedObject(Cdn.Object obj)
		{
			DisconnectWrapped();
			d_object = obj;
			ConnectWrapped();
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
		
		public Cdn.Variable this[string name]
		{
			get
			{
				return Variable(name);
			}
		}
		
		public Cdn.Variable Variable(string name)
		{
			return d_object.Variable(name);
		}
		
		public Wrapper[] AppliedTemplates
		{
			get
			{
				return Wrap(d_object.AppliedTemplates);
			}
		}
		
		public bool HasVariable(string name)
		{
			return d_object.HasVariable(name);
		}
		
		public bool AddVariable(Cdn.Variable property)
		{
			return d_object.AddVariable(property);
		}
		
		public Cdn.Variable AddVariable(string name, string val, Cdn.VariableFlags flags)
		{
			Cdn.Variable prop = new Cdn.Variable(name, new Cdn.Expression(val), flags);
			
			if (AddVariable(prop))
			{
				return prop;
			}
			else
			{
				return null;
			}
		}
		
		public Cdn.Variable AddVariable(string name, string val)
		{
			return AddVariable(name, val, Cdn.VariableFlags.None);
		}
		
		public bool RemoveVariable(string name)
		{
			return d_object.RemoveVariable(name);
		}
		
		public bool IsCompiled
		{
			get
			{
				return d_object.IsCompiled;
			}
		}
		
		public Node Parent
		{
			get
			{
				return Wrap(d_object.Parent) as Node;
			}
		}
		
		public Cdn.Variable[] Variables
		{
			get
			{
				return d_object.Variables;
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

		private void ExtractAnnotationInfo()
		{
			var annotatable = d_object as Cdn.Annotatable;

			if (annotatable != null && !String.IsNullOrEmpty(annotatable.Annotation))
			{
				d_annotationInfo = annotatable.ParseAnnotation();

				if (!String.IsNullOrEmpty(d_annotationInfo.RelativeTo))
				{
					var node = d_object as Cdn.Node;

					if (node == null)
					{
						node = d_object.Parent;
					}

					if (node != null)
					{
						d_annotationRelative = new List<Cdn.Object>(node.FindObjects(d_annotationInfo.RelativeTo));
					}
				}
			}
			else
			{
				d_annotationInfo = null;
				d_annotationRelative = new List<Cdn.Object>();
			}
		}
		
		private void NotifyAnnotationHandler(object source, GLib.NotifyArgs args)
		{
			ExtractAnnotationInfo();

			DoRequestRedraw();
		}
		
		private void HandleVariableAdded(object o, Cdn.VariableAddedArgs args)
		{
			VariableAdded(this, args.Variable);
		}
		
		private void HandleVariableRemoved(object o, Cdn.VariableRemovedArgs args)
		{
			VariableRemoved(this, args.Variable);
		}
		
		private void HandleVariableChanged(object o, Cdn.ExpressionChangedArgs args)
		{
			// TODO
			VariableChanged(this, (Cdn.Variable)o);
		}
		
		protected virtual void DisconnectWrapped()
		{
			WrappedObject.RemoveNotification("id", NotifyIdHandler);
			
			if (WrappedObject is Cdn.Annotatable)
			{
				WrappedObject.RemoveNotification("annotation", NotifyAnnotationHandler);
			}

			WrappedObject.VariableAdded -= HandleVariableAdded;
			WrappedObject.VariableRemoved -= HandleVariableRemoved;
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
			
			if (WrappedObject is Cdn.Annotatable)
			{
				WrappedObject.AddNotification("annotation", NotifyAnnotationHandler);
			}
				
			WrappedObject.VariableAdded += HandleVariableAdded;
			WrappedObject.VariableRemoved += HandleVariableRemoved;
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
		
		public virtual Cdn.Object WrappedObject
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
		
		public bool Compile(Cdn.CompileContext context)
		{
			return Compile(context, null);
		}
		
		public bool Compile(Cdn.CompileContext context, Cdn.CompileError error)
		{
			return WrappedObject.Compile(context, error);
		}
		
		public void Reset()
		{
			WrappedObject.Reset();
		}
	
		public List<Wrappers.Edge> Links
		{
			get
			{
				return d_links;
			}
		}
		
		public void Link(Wrappers.Edge link)
		{
			if (!d_links.Contains(link))
			{
				d_links.Add(link);
			}
		}
		
		public void Unlink(Wrappers.Edge link)
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
			foreach (Wrappers.Edge link in d_links)
			{
				link.DoRequestRedraw();
			}
				
			base.DoRequestRedraw();
			
			foreach (Wrappers.Edge link in d_links)
			{
				link.DoRequestRedraw();
			}
		}
		
		public bool VerifyRemoveVariable(string prop)
		{
			return WrappedObject.VerifyRemoveVariable(prop);
		}
		
		public Wrappers.Wrapper GetVariableTemplate(Cdn.Variable prop, bool matchFull)
		{
			return Wrappers.Wrapper.Wrap(WrappedObject.GetVariableTemplate(prop, matchFull));
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
		
		public Wrappers.Node TopParent
		{
			get
			{
				if (Parent == null)
				{
					return this as Wrappers.Node;
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
			x = 0;
			y = 0;
		}

		public virtual void DrawAnnotation(Cairo.Context context, Cdn.AnnotationInfo info)
		{
			var uw = context.LineWidth;
			var alloc = AnnotationAllocation(1 / uw, context);

			alloc.Offset(-Allocation.X / uw, -Allocation.Y / uw);

			context.Save();
			context.Scale(context.LineWidth, context.LineWidth);
			context.LineWidth = 1;

			context.Rectangle(alloc.X, alloc.Y, alloc.Width, alloc.Height);
			context.SetSourceRGBA(1, 1, 1, 0.75);
			context.Fill();

			context.Rectangle(alloc.X + 2, alloc.Y + 2, alloc.Width - 4, alloc.Height - 4);
			context.SetSourceRGB(0.95, 0.95, 0.95);
			context.SetDash(new double[] {5, 5}, 0);
			context.Stroke();

			Pango.Layout layout = Pango.CairoHelper.CreateLayout(context);
			Pango.CairoHelper.UpdateLayout(context, layout);
			layout.FontDescription = Settings.Font;

			layout.SetText(info.Text.Trim());

			context.MoveTo(alloc.X + 2, alloc.Y + 2);
			context.SetSourceRGB(0.5, 0.5, 0.5);
			Pango.CairoHelper.ShowLayout(context, layout);

			context.Restore();
		}

		public override void Draw(Cairo.Context context)
		{
			base.Draw(context);
			
			if (d_annotationInfo != null)
			{
				DrawAnnotation(context, d_annotationInfo);
			}
		}

		private Allocation AnnotationAllocation(double scale, Cairo.Context graphics)
		{
			if (d_annotationInfo == null)
			{
				return null;
			}

			double x = 0;
			double y = 0;

			if (d_annotationRelative != null && d_annotationRelative.Count > 0)
			{
				foreach (var wrap in d_annotationRelative)
				{
					int lx;
					int ly;

					wrap.GetLocation(out lx, out ly);

					x += lx;
					y += ly;
				}

				x /= d_annotationRelative.Count;
				y /= d_annotationRelative.Count;
			}
			else
			{
				x = Allocation.X + Allocation.Width / 2;
				y = Allocation.Y + Allocation.Height / 2;
			}

			double ax;
			double ay;

			if (!d_annotationInfo.GetLocation(out ax, out ay))
			{
				ax = 0;
				ay = -0.5;
			}

			x += ax;
			y += ay;

			int w;
			int h;

			MeasureString(graphics, d_annotationInfo.Text.Trim(), out w, out h);

			Allocation ret = new Cdn.Studio.Allocation(x, y, w / scale, h / scale);

			var anchor = d_annotationInfo.Anchor;

			switch (anchor)
			{
			case Cdn.AnnotationAnchor.North:
			case AnnotationAnchor.NorthWest:
			case AnnotationAnchor.NorthEast:
				ret.Y -= ret.Height;
				break;
			case AnnotationAnchor.West:
			case AnnotationAnchor.East:
			case AnnotationAnchor.Center:
				ret.Y -= ret.Height * 0.5;
				break;
			}

			switch (anchor)
			{
			case AnnotationAnchor.NorthWest:
			case AnnotationAnchor.SouthWest:
			case AnnotationAnchor.West:
				ret.X += Allocation.Width * 0.5;
				break;
			case AnnotationAnchor.South:
			case AnnotationAnchor.North:
			case AnnotationAnchor.Center:
				ret.X -= Allocation.Width * 0.5 + ret.Width;
				break;
			}

			ret.Scale(scale);
			ret.GrowBorder(4);

			return ret;
		}

		public override Allocation Extents(double scale, Cairo.Context graphics)
		{
			var ext = base.Extents(scale, graphics);
			var anext = AnnotationAllocation(scale, graphics);

			if (anext != null)
			{
				return ext.Extend(anext);
			}
			else
			{
				return ext;
			}
		}
	}
}
