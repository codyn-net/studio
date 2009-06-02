using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Reflection;

namespace Cpg.Studio.Serialization
{
	public class Saver
	{
		Cpg d_cpg;
		
		public Saver(Window window, List<Components.Object> objects)
		{
			Components.Group group = new Components.Group();
			
			foreach (Components.Object obj in objects)
				group.Add(obj);
		
			Initialize(window, group);
		}
		
		public Saver(Window window, Components.Group group)
		{
			Initialize(window, group);
		}
		
		public void Initialize(Window window, Components.Group group)
		{
			d_cpg = new Cpg();

			d_cpg.Network.Objects.AddRange(Collect(group));
			d_cpg.Project.Window = window;
			d_cpg.Project.Root = new Group(group);
		}
		
		private XmlWriterSettings WriterSettings()
		{
			XmlWriterSettings settings = new XmlWriterSettings();

			settings.Indent = true;
			settings.NewLineOnAttributes = false;
			
			return settings;
		}
		
		private XmlSerializerNamespaces Namespace()
		{
			XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
			ns.Add("cpg", "http://birg.epfl.ch/cpg");
				
			return ns;
		}
		
		private Type[] ResolveSubclasses(Type t)
		{
			List<Type> types = new List<Type>();
			
			types.Add(t);
			
			foreach (Type sub in Assembly.GetEntryAssembly().GetTypes())
			{
				if (sub.IsSubclassOf(t))
					types.Add(sub);
			}
			
			return types.ToArray();
		}
				
		private void Ignore<T>(XmlAttributeOverrides overrides, params string[] names)
		{
			XmlAttributes attrs = new XmlAttributes();
			attrs.XmlIgnore = true;
			
			Type[] types = ResolveSubclasses(typeof(T));
			
			foreach (string name in names)
			{
				foreach (Type t in types)
				{
					overrides.Add(t, name, attrs);
				}
			}
			
			if (names.Length == 0)
			{
				foreach (Type t in types)
				{
					overrides.Add(t, attrs);
				}
			}
		}
		
		private XmlAttributeOverrides OverrideNetwork()
		{
			XmlAttributeOverrides overrides = new XmlAttributeOverrides();
			
			Ignore<Cpg>(overrides, "Project");
			Ignore<Object>(overrides, "Allocation");
			Ignore<Group>(overrides);

			return overrides;
		}
		
		private XmlAttributeOverrides OverrideProject()
		{
			XmlAttributeOverrides overrides = new XmlAttributeOverrides();
			
			Ignore<Cpg>(overrides, "Network");
			Ignore<Simulated>(overrides, "Properties");
			Ignore<Link>(overrides, "From", "To", "Actions");
			
			return overrides;
		}
		
		public string Save()
		{
			XmlWriterSettings settings = WriterSettings();
			
			/* Network */
			StringWriter networkStream = new StringWriter();
			XmlWriter writer;
			XmlSerializer serializer;
			
			writer = XmlTextWriter.Create(networkStream, settings);
			serializer = new XmlSerializer(typeof(Cpg), OverrideNetwork());

			serializer.Serialize(writer, d_cpg, Namespace());
			
			/* Project */
			StringWriter projectStream = new StringWriter();
			writer = XmlTextWriter.Create(projectStream, settings);
			serializer = new XmlSerializer(typeof(Cpg), OverrideProject());

			serializer.Serialize(writer, d_cpg, Namespace());
			
			/* Create Document for network */
			XmlDocument network = new XmlDocument();
			network.LoadXml(networkStream.ToString());
			
			/* Create Document for project */
			XmlDocument project = new XmlDocument();
			project.LoadXml(projectStream.ToString());
			
			XmlNode node = network.ImportNode(project.SelectSingleNode("//cpg/project"), true);
			network.SelectSingleNode("//cpg").AppendChild(node);
			
			StringWriter stream = new StringWriter();
			writer = XmlTextWriter.Create(stream, settings);
			
			network.Save(writer);
			
			return stream.ToString();
		}
		
		private static void WriteToStream(Stream stream, string doc)
		{
			TextWriter writer = new StreamWriter(stream, Encoding.UTF8);
			writer.Write(doc);
			writer.Write("\n");

			writer.Close();
		}
		
		private static void TempMove(string doc, string filename)
		{
			string tmp = Path.GetTempFileName();
			
			FileStream f = new FileStream(tmp, FileMode.OpenOrCreate, FileAccess.Write);
			WriteToStream(f, doc);
			f.Close();

			if (File.Exists(filename + "~"))
				File.Delete(filename + "~");
			
			File.Move(filename, filename + "~");
			File.Move(tmp, filename);
		}
		
		public static void SaveToFile(string filename, string doc)
		{
			FileStream f;
			
			try
			{
				f = new FileStream(filename, FileMode.CreateNew, FileAccess.Write);
			}
			catch (IOException)
			{
				TempMove(doc, filename);
				return;
			}
			
			WriteToStream(f, doc);
			f.Close();
		}
		
		public void Save(string filename)
		{
			SaveToFile(filename, Save());			
		}
		
		private List<Simulated> Collect(List<Components.Object> objects)
		{
			List<Simulated> list = new List<Simulated>();
			
			foreach (Components.Object obj in objects)
			{
				if (obj is Components.Group)
				{
					list.AddRange(Collect((obj as Components.Group).Children));
				}
				else
				{
					Object o = Object.Create(obj);
					
					if (o != null && o is Simulated)
						list.Add(o as Simulated);
				}
			}

			list.Sort();
			return list;
		}
		
		private List<Simulated> Collect(Components.Group group)
		{
			List<Components.Object> objects = new List<Components.Object>();
			
			objects.Add(group);
			return Collect(objects);
		}
	}
}
