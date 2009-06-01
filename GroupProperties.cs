using System;
using Gtk;
using System.Collections.Generic;
using System.Reflection;

namespace Cpg.Studio
{
	public class GroupProperties : Gtk.Table
	{
		private List<Components.Simulated> d_objects;
		private ComboBox d_comboMain;
		private ComboBox d_comboKlass;
		
		public GroupProperties(Components.Object[] objects, Components.Simulated defmain, Type defklass) : base(2, 2, false)
		{
			RowSpacing = 6;
			ColumnSpacing = 12;
			
			/* Get all simulated non-links */
			d_objects = new List<Components.Simulated>();
			
			foreach (Components.Object obj in objects)
			{
				if (obj is Components.Simulated && !(obj is Components.Link))
					d_objects.Add(obj as Components.Simulated);
			}
			
			Build(defmain, defklass);
			ShowAll();
		}
		
		public GroupProperties(Components.Object[] objects) : this(objects, null, null)
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
			
			Components.Renderers.Group renderer = info.Invoke(new object[] {}) as Components.Renderers.Group;
			
			return renderer.Icon(24);
		}
		
		private void Build(Components.Simulated defmain, Type defklass)
		{
			Attach(MakeLabel("Relay:"), 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			Attach(MakeLabel("Class:"), 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			
			ListStore store = new ListStore(typeof(Components.Simulated), typeof(String), typeof(Gdk.Pixbuf));
			TreePath path = null;
			
			foreach (Components.Simulated obj in d_objects)
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
				if (!type.IsSubclassOf(typeof(Components.Renderers.Group)))
					continue;

				object[] attributes = type.GetCustomAttributes(typeof(Components.Renderers.NameAttribute), true);
				string name;
				
				if (attributes.Length != 0)
				{
					name = (attributes[0] as Components.Renderers.NameAttribute).Name;
				}
				else
				{
					name = "None";	
				}

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
		
		public Components.Simulated Main
		{
			get
			{
				TreeIter iter;
				
				if (d_comboMain.GetActiveIter(out iter))
					return d_comboMain.Model.GetValue(iter, 0) as Components.Simulated;
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
