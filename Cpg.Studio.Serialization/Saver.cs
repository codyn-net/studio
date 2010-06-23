using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Reflection;

using CCpg = Cpg;

namespace Cpg.Studio.Serialization
{
	public class Saver
	{
		Cpg d_cpg;
		
		public Saver(Window window, List<Wrappers.Wrapper> objects)
		{
			Wrappers.Group group = new Wrappers.Group();
			
			foreach (Wrappers.Wrapper obj in objects)
				group.Add(obj);
		
			Initialize(window, group);
		}
		
		public Saver(Window window, Wrappers.Group group)
		{
			Initialize(window, group);
		}
		
		public void Initialize(Window window, Wrappers.Group group)
		{
			CCpg.Network network = new CCpg.Network();
			network.Merge(window.Network);
			
			Dictionary<string, bool> selection = Collect(group);
			
			foreach (CCpg.Object o in network.States)
			{
				if (!selection.ContainsKey(o.Id))
				{
					network.RemoveObject(o);
				}
			}
			
			foreach (CCpg.Link o in network.Links)
			{
				if (!selection.ContainsKey(o.Id))
				{
					network.RemoveObject(o);
				}
			}

			d_cpg = new Cpg(network);
			
			d_cpg.Project.Window = window;
			d_cpg.Project.Root = new Group(group);
		}
		
		private XmlWriterSettings WriterSettings()
		{
			XmlWriterSettings settings = new XmlWriterSettings();

			settings.Indent = true;
			settings.NewLineOnAttributes = false;
			settings.Encoding = Encoding.UTF8;
			
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
		
		private XmlAttributeOverrides OverrideProject()
		{
			XmlAttributeOverrides overrides = new XmlAttributeOverrides();
			
			Ignore<Cpg>(overrides, "Network");
			Ignore<Simulated>(overrides, "Properties");
			Ignore<Link>(overrides, "From", "To", "Actions");
			
			return overrides;
		}
		
		private XmlDocument MakeDocument(XmlAttributeOverrides overrides)
		{
			XmlWriterSettings settings = WriterSettings();
			
			XmlWriter writer;
			XmlSerializer serializer;
			MemoryStream memstream;
			StreamWriter stream;
			
			memstream = new MemoryStream();
			stream = new StreamWriter(memstream);
			
			writer = XmlWriter.Create(stream, settings);
			serializer = new XmlSerializer(typeof(Cpg), overrides);

			serializer.Serialize(writer, d_cpg, Namespace());
			
			XmlDocument doc = new XmlDocument();
			memstream.Position = 0;
			doc.Load(new StreamReader(memstream));
			
			return doc;
		}
		
		private XmlDocument MakeNetworkDocument()
		{
			StringReader stream = new StringReader(d_cpg.Network.CNetwork.WriteToXml());
			XmlDocument doc = new XmlDocument();
			doc.Load(stream);
			stream.Close();
			
			return doc;
		}
		
		public string Save()
		{
			XmlDocument network = MakeNetworkDocument();
			XmlDocument project = MakeDocument(OverrideProject());
			
			XmlNode node = network.ImportNode(project.SelectSingleNode("//cpg/project"), true);
			network.SelectSingleNode("//cpg").AppendChild(node);
			
			StringWriter stream = new StringWriter();
			XmlWriter writer = XmlTextWriter.Create(stream, WriterSettings());
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
		
		private void Collect(List<Wrappers.Wrapper> objects, Dictionary<string, Wrappers.Wrapper> ret)
		{
			foreach (Wrappers.Wrapper obj in objects)
			{
				if (obj is Wrappers.Group)
				{
					Collect((obj as Wrappers.Group).Children, ret);
				}
				else
				{
					CCpg.Simulated sim = obj as CCpg.Simulated;
					
					if (sim != null)
					{
						ret[sim.FullId] = true;
					}
				}
			}

			list.Sort();
			return list;
		}
		
		private Dictionary<string, bool> Collect(Wrappers.Group group)
		{
			List<Wrappers.Wrapper> objects = new List<Wrappers.Wrapper>();
			
			objects.Add(group);
			return Collect(objects);
		}
	}
}
