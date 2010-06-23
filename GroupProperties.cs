using System;
using Gtk;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio
{
	public class GroupProperties : Gtk.Table
	{
		private List<Wrappers.Wrapper> d_objects;
		private ComboBox d_comboMain;
		private ComboBox d_comboKlass;
		
		public GroupProperties(Wrappers.Wrapper[] objects, Wrappers.Wrapper defmain, Type defklass) : base(2, 2, false)
		{
			RowSpacing = 6;
			ColumnSpacing = 12;
			
			/* Get all simulated non-links */
			d_objects = new List<Wrappers.Wrapper>();
			
			foreach (Wrappers.Wrapper obj in objects)
			{
				if (obj is Wrappers.Wrapper && !(obj is Wrappers.Link))
					d_objects.Add(obj as Wrappers.Wrapper);
			}
			
			Build(defmain, defklass);
			ShowAll();
		}
		
		public GroupProperties(Wrappers.Wrapper[] objects) : this(objects, null, null)
		{
		}
		
		private Widget MakeLabel(string text)
		{
			Label label = new Label(text);
			label.Xalign = 0;
			
			return label;
		}
		
		private Gdk.Pixbuf GroupIcon(Type type)
		{
			ConstructorInfo info = type.GetConstructor(new Type[] {});
			
			if (info == null)
				return null;
			
			Wrappers.Renderers.Group renderer = info.Invoke(new object[] {}) as Wrappers.Renderers.Group;
			
			return renderer.Icon(24);
		}
		
		private void Build(Wrappers.Wrapper defmain, Type defklass)
		{
			Attach(MakeLabel("Proxy:"), 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			Attach(MakeLabel("Class:"), 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			
			ListStore store = new ListStore(typeof(Wrappers.Wrapper), typeof(String), typeof(Gdk.Pixbuf));
			TreePath path = null;
			
			foreach (Wrappers.Wrapper obj in d_objects)
			{
				if (String.IsNullOrEmpty(obj.Id))
					continue;
				
				TreeIter iter = store.Append();
				store.SetValue(iter, 0, new GLib.Value(obj));
				store.SetValue(iter, 1, new GLib.Value(obj.Id));
				
				if (obj.Renderer != null)
					store.SetValue(iter, 2, new GLib.Value(obj.Renderer.Icon(24)));
				
				if (obj == defmain)
					path = store.GetPath(iter);
			}
			
			ComboBox cmb = new ComboBox(store);
			CellRendererPixbuf iconrenderer = new CellRendererPixbuf();
			cmb.PackStart(iconrenderer, false);
			cmb.AddAttribute(iconrenderer, "pixbuf", 2);
			
			CellRendererText textrenderer = new CellRendererText();
			cmb.PackStart(textrenderer, true);
			cmb.AddAttribute(textrenderer, "text", 1);
			textrenderer.Yalign = 0.5f;
			
			if (path != null)
			{
				TreeIter it;
				store.GetIter(out it, path);
				cmb.SetActiveIter(it);
			}
			else
			{
				cmb.Active = 0;
			}
			
			d_comboMain = cmb;
			Attach(cmb, 1, 2, 0, 1);
			
			store = new ListStore(typeof(Type), typeof(String), typeof(Gdk.Pixbuf));
			cmb = new ComboBox(store);
			path = null;
			
			Assembly asm = Assembly.GetEntryAssembly();
			
			foreach (Type type in asm.GetTypes())
			{
				if (!type.IsSubclassOf(typeof(Wrappers.Renderers.Group)))
					continue;

				string name = Wrappers.Renderers.Renderer.GetName(type);
				TreeIter iter = store.Append();
				
				store.SetValue(iter, 0, new GLib.Value(type));
				store.SetValue(iter, 1, new GLib.Value(name));
				store.SetValue(iter, 2, new GLib.Value(GroupIcon(type)));

				if (type == defklass)
					path = store.GetPath(iter);
			}
			
			iconrenderer = new CellRendererPixbuf();
			cmb.PackStart(iconrenderer, false);
			cmb.AddAttribute(iconrenderer, "pixbuf", 2);

			textrenderer = new CellRendererText();
			cmb.PackStart(textrenderer, true);
			cmb.AddAttribute(textrenderer, "text", 1);
			textrenderer.Yalign = 0.5f;
			
			if (path != null)
			{
				TreeIter it;
				store.GetIter(out it, path);
				cmb.SetActiveIter(it);
			}
			else
			{
				cmb.Active = 0;	
			}
			
			d_comboKlass = cmb;
			Attach(cmb, 1, 2, 1, 2);
		}
		
		public Wrappers.Wrapper Main
		{
			get
			{
				TreeIter iter;
				
				if (d_comboMain.GetActiveIter(out iter))
					return d_comboMain.Model.GetValue(iter, 0) as Wrappers.Wrapper;
				else
					return null;
			}
		}
		
		public Type Klass
		{
			get
			{
				TreeIter iter;
				
				if (d_comboKlass.GetActiveIter(out iter))
					return d_comboKlass.Model.GetValue(iter, 0) as Type;
				else
					return null;
			}
		}
		
		public ComboBox ComboMain
		{
			get
			{
				return d_comboMain;
			}
		}
		
		public ComboBox ComboKlass
		{
			get
			{
				return d_comboKlass;
			}
		}
	}
}
