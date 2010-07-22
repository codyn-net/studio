using System;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cpg.Studio.Serialization
{
	public class Project
	{
		[XmlType("show")]
		public class Show
		{
			[XmlElement("tool-bar")]
			public bool ToolBar;
			
			[XmlElement("path-bar")]
			public bool PathBar;
			
			[XmlElement("property-pane")]
			public bool PropertyPane;
			
			[XmlElement("simulate-bar")]
			public bool SimulateBar;
			
			[XmlElement("status-bar")]
			public bool StatusBar;
			
			public Show()
			{
				ToolBar = true;
				PathBar = true;
				PropertyPane = true;
				SimulateBar = true;
				StatusBar = true;
			}
		}
		
		private Show d_show;
		private string d_filename;
		private string d_externalProjectFile;
		private Wrappers.Network d_network;
		private Network d_metaNetwork;
		private Templates d_metaTemplates;
		
		public Project()
		{
			Clear();
			
			d_network = new Wrappers.Network();
		}

		public string ExternalProjectFile
		{
			get
			{
				return d_externalProjectFile;
			}
		}
		
		public string Filename
		{
			get
			{
				return d_filename;
			}
			set
			{
				d_filename = null;
			}
		}
		
		public bool ProjectIsExternal
		{
			get
			{
				return d_externalProjectFile != null;
			}
		}
		
		public Wrappers.Network Network
		{
			get
			{
				return d_network;
			}
		}
		
		public Network MetaNetwork
		{
			get
			{
				return d_metaNetwork;
			}
		}
		
		public Templates MetaTemplates
		{
			get
			{
				return d_metaTemplates;
			}
		}
		
		public Show ShowSettings
		{
			get
			{
				return d_show;
			}
		}
		
		public void Clear()
		{
			d_metaNetwork = new Network();
			d_metaTemplates = new Templates();

			d_show = new Show();

			d_externalProjectFile = null;
			d_filename = null;
		}
		
		private Type TypeMap(Type origType)
		{
			Type[,] types = new Type[,] {
				{typeof(Wrappers.Group), typeof(Group)},
				{typeof(Wrappers.Link), typeof(Link)},
				{typeof(Wrappers.State), typeof(State)},
				{typeof(Wrappers.Network), typeof(Network)}
			};
			
			for (int i = 0; i <= types.GetUpperBound(0); ++i)
			{
				if (types[i, 0] == origType)
				{
					return types[i, 1];
				}
			}
			
			return null;
		}
		
		private void Merge(Wrappers.Wrapper orig, Object meta)
		{
			orig.Allocation = meta.Allocation.Copy();
		}
		
		private void Merge(Wrappers.Group orig, Group meta, Dictionary<Wrappers.Group, List<Wrappers.Wrapper>> missing)
		{
			orig.X = meta.X;
			orig.Y = meta.Y;
			
			List<Wrappers.Wrapper> origChildren = new List<Wrappers.Wrapper>(orig.Children);
			
			foreach (Object o in meta.Children)
			{
				Wrappers.Wrapper origObj = orig.FindObject(o.Id);
				Type t = TypeMap(origObj.GetType());
				
				if (t == null || t != o.GetType())
				{
					continue;
				}
				
				origChildren.Remove(origObj);
				
				Merge(origObj, o);
				
				if (origObj is Wrappers.Group)
				{
					Merge((Wrappers.Group)origObj, (Group)o, missing);
				}
			}
			
			missing[orig] = origChildren;
		}
		
		private void Merge()
		{
			Dictionary<Wrappers.Group, List<Wrappers.Wrapper>> missing = new Dictionary<Wrappers.Group, List<Wrappers.Wrapper>>();
			
			Merge(d_network, d_metaNetwork, missing);
			
			Dictionary<Wrappers.Group, List<Wrappers.Wrapper>> missingTemplates = new Dictionary<Wrappers.Group, List<Wrappers.Wrapper>>();
			Merge(d_network.TemplateGroup, d_metaTemplates, missingTemplates);
			
			// Now do some layouting on the missing guys?
		}
		
		private string GenerateProjectFilename(string filename)
		{
			return Path.Combine(Path.GetDirectoryName(filename), "." + Path.GetFileName(filename) + "-project");
		}
		
		public void Load(string filename)
		{
			d_filename = filename;
			d_network = new Wrappers.Network(filename);

			XmlDocument doc = new XmlDocument();
			doc.Load(filename);
			
			XmlNode projectNode;
			
			projectNode = doc.SelectSingleNode("/cpg/project");
			
			if (projectNode == null)
			{
				// Try to load external project doc
				string extfile = GenerateProjectFilename(filename);
				
				if (File.Exists(extfile))
				{
					doc.Load(extfile);
					projectNode = doc.SelectSingleNode("/cpg/project");
					d_externalProjectFile = extfile;
				}
			}
			
			if (projectNode != null)
			{
				ExtractAnnotations(projectNode);
			}
			
			// Merge network and metadata
			Merge();
		}
		
		private void ExtractAnnotations(XmlNode node)
		{
			// Extract show settings
			XmlNode show = node.SelectSingleNode("show");
			
			if (show != null)
			{
				d_show = Deserialize<Project.Show>(show);
			}
			
			// Extract the object metadata
			XmlNode net = node.SelectSingleNode("network");
			
			if (net != null)
			{
				d_metaNetwork = Deserialize<Network>(net);
			}
			
			// Extract the template metadata
			XmlNode temp = node.SelectSingleNode("templates");
			
			if (temp != null)
			{
				d_metaTemplates = Deserialize<Templates>(temp);
			}
		}

		private T Deserialize<T>(XmlNode node)
		{
			XmlNodeReader reader = new XmlNodeReader(node);
			XmlSerializer ser = new XmlSerializer(typeof(T));
			
			return (T)ser.Deserialize(reader);
		}
		
		public void Save()
		{
			Save(d_filename);
		}
		
		private void CreateMeta(Wrappers.Group orig, Group meta)
		{
			meta.X = orig.X;
			meta.Y = orig.Y;

			foreach (Wrappers.Wrapper child in orig.Children)
			{
				Type t = TypeMap(child.GetType());
				
				if (t == null)
				{
					throw new Exception(String.Format("I do not know how to serialize {0}!!!", child));
				}
				
				Object obj = (Object)t.GetConstructor(new Type[] {}).Invoke(new object[] {});
				
				obj.Id = child.Id;
				obj.Allocation = child.Allocation.Copy();
				
				meta.Children.Add(obj);
				
				if (obj is Group)
				{
					CreateMeta((Wrappers.Group)child, (Group)obj);
				}
			}
		}
		
		private void CreateMeta()
		{
			d_metaNetwork = new Network();
			CreateMeta(d_network, d_metaNetwork);
			
			d_metaTemplates = new Templates();
			CreateMeta(d_network.TemplateGroup, d_metaTemplates);
		}
		
		private XmlWriterSettings WriterSettings()
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			
			settings.Indent = true;
			settings.NewLineOnAttributes = false;
			settings.Encoding = new UTF8Encoding(false);
			
			return settings;
		}
		
		private void WriteToStream(Stream stream, XmlDocument doc)
		{
			XmlWriter xmlWriter = XmlTextWriter.Create(stream, WriterSettings());
			
			doc.Save(xmlWriter);
			xmlWriter.Close();
		}
		
		private void TempMove(XmlDocument doc, string filename)
		{
			string tmp = Path.GetTempFileName();
			
			FileStream f = new FileStream(tmp, FileMode.OpenOrCreate, FileAccess.Write);
			WriteToStream(f, doc);
			f.Close();
			
			if (File.Exists(filename + "~"))
			{
				File.Delete(filename + "~");
			}
			
			File.Move(filename, filename + "~");
			File.Move(tmp, filename);
		}
		
		private void Save(XmlDocument doc, string filename)
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
			Cpg.NetworkSerializer serializer = new Cpg.NetworkSerializer(d_network, d_network.WrappedObject);
			
			CreateMeta();
			
			XmlNode networkNode = Serialize(d_metaNetwork);
			XmlNode templatesNode = Serialize(d_metaTemplates);
			
			if (ProjectIsExternal)
			{
				// Write to separate files
				string xml = serializer.SerializeMemory();
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);

				Save(doc, filename);
				
				string extfile = GenerateProjectFilename(filename);
				
				XmlDocument ext = new XmlDocument();
				XmlElement root = ext.CreateElement("cpg");
				ext.AppendChild(root);
				
				XmlElement projectNode = ext.CreateElement("project");
				
				projectNode.AppendChild(ext.ImportNode(networkNode, true));
				projectNode.AppendChild(ext.ImportNode(templatesNode, true));
				
				root.AppendChild(projectNode);
				
				ext.Save(extfile);
				
				d_externalProjectFile = extfile;
			}
			else
			{
				string xml = serializer.SerializeMemory();
			
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);
			
				// Append a project xml
				XmlNode root = doc.SelectSingleNode("cpg");
				
				XmlElement projectNode = doc.CreateElement("project");
				projectNode.AppendChild(doc.ImportNode(networkNode, true));
				projectNode.AppendChild(doc.ImportNode(templatesNode, true));

				root.AppendChild(projectNode);
				Save(doc, filename);
			}
			
			d_filename = filename;
		}
		
		private XmlNode Serialize<T>(T obj)
		{
			XmlSerializer ser = new XmlSerializer(typeof(T));
			XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
			ns.Add("", "");
			
			XmlDocument doc = new XmlDocument();
			XPathNavigator nav = doc.CreateNavigator();
			
			XmlWriter writer = nav.AppendChild();
			
			ser.Serialize(writer, obj, ns);
			
			//writer.WriteEndDocument();
			writer.Flush();
			writer.Close();
			
			return doc.FirstChild;
		}		
	}
}

