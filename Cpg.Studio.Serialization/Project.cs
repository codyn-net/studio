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
		[XmlType("monitor")]
		public class Monitor
		{
			[XmlElement("id")]
			public List<string> Id;
			
			public Monitor()
			{
				Id = new List<string>();
			}
			
			public Monitor(params string[] id)
			{
				Id = new List<string>(id);
			}
		}

		[XmlType("settings")]
		public class SettingsType
		{
			[XmlElement("tool-bar")]
			public bool ToolBar;
			
			[XmlElement("path-bar")]
			public bool PathBar;
					
			[XmlElement("simulate-bar")]
			public bool SimulateBar;
			
			[XmlElement("status-bar")]
			public bool StatusBar;
			
			[XmlElement("annotations")]
			public bool Annotations;

			[XmlElement("pane-position")]
			public int PanePosition;
			
			[XmlElement("annotation-pane-position")]
			public int AnnotationPanePosition;
			
			[XmlElement("simulate-period")]
			public string SimulatePeriod;
			
			[XmlElement("allocation")]
			public Allocation Allocation;
			
			[XmlElement("active-group")]
			public string ActiveGroup;
			
			[XmlElement("active-root")]
			public string ActiveRoot;
			
			public struct MonitorsType
			{
				[XmlElement("graph"),
				 XmlElement(typeof(Monitor))]
				public List<Monitor> Graphs;
				
				[XmlAttribute("rows")]
				public uint Rows;
				
				[XmlAttribute("columns")]
				public uint Columns;
				
				[XmlElement("allocation")]
				public Allocation Allocation;
			}
			
			[XmlElement("monitors")]
			public MonitorsType Monitors;
			
			public SettingsType()
			{
				ToolBar = true;
				PathBar = true;
				SimulateBar = true;
				StatusBar = true;
				Annotations = false;
				PanePosition = 250;
				AnnotationPanePosition = 250;
				SimulatePeriod = "0:0.01:1";
				Allocation = new Allocation(-1, -1, 700, 600);
				ActiveGroup = "";

				Monitors.Graphs = new List<Monitor>();
				Monitors.Rows = 0;
				Monitors.Columns = 0;
			}
		}
		
		private SettingsType d_settings;
		private string d_filename;
		private bool d_saveProjectExternally;
		private string d_externalProjectFile;
		private Wrappers.Network d_network;

		private Network d_metaNetwork;
		private Templates d_metaTemplates;
		private Functions d_metaFunctions;
		private string d_externalPath;
		private bool d_shared;
		private bool d_cansave;
		
		public Project()
		{
			Clear();
			
			d_network = new Wrappers.Network();
			d_saveProjectExternally = false;
			d_shared = false;
		}
		
		[XmlAttribute("path")]
		public string ExternalPath
		{
			get
			{	
				return d_externalPath;
			}
			set
			{
				d_externalPath = value;
			}
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
		
		public bool SaveProjectExternally
		{
			get
			{
				return d_saveProjectExternally;
			}
			set
			{
				d_saveProjectExternally = value;
			}
		}
		
		public bool CanSave
		{
			get
			{
				return d_cansave;
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
		
		public SettingsType Settings
		{
			get
			{
				return d_settings;
			}
		}
		
		public void Clear()
		{
			d_metaNetwork = new Network();
			d_metaTemplates = new Templates();
			d_metaFunctions = new Functions();

			d_settings = new SettingsType();

			d_externalProjectFile = null;
			d_filename = null;
		}
		
		private Type TypeMap(Type origType)
		{
			Type[,] types = new Type[,] {
				{typeof(Wrappers.Group), typeof(Group)},
				{typeof(Wrappers.Link), typeof(Link)},
				{typeof(Wrappers.Object), typeof(State)},
				{typeof(Wrappers.Network), typeof(Network)},
				{typeof(Wrappers.FunctionPolynomial), typeof(FunctionPolynomial)},
				{typeof(Wrappers.InputFile), typeof(InputFile)}
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
		
		private void Merge(Wrappers.Group orig, Group meta, Dictionary<Wrappers.Group, List<Wrappers.Wrapper>> missing)
		{
			List<Wrappers.Wrapper> origChildren = new List<Wrappers.Wrapper>(orig.Children);
			
			foreach (Object o in meta.Children)
			{
				Wrappers.Wrapper origObj = orig.FindObject(o.Id);
				
				if (origObj == null)
				{
					// Ignore
					continue;
				}

				Type t = TypeMap(origObj.GetType());
				
				if (t == null || t != o.GetType())
				{
					continue;
				}
				
				origChildren.Remove(origObj);
				
				o.Merge(origObj);
				
				if (origObj is Wrappers.Group)
				{
					Merge((Wrappers.Group)origObj, (Group)o, missing);
				}
			}
			
			if (missing != null)
			{
				missing[orig] = origChildren;
			}
		}
		
		private void Merge(Wrappers.Group orig, Group meta)
		{
			Merge(orig, meta, null);
		}
		
		private void Merge()
		{
			Dictionary<Wrappers.Group, List<Wrappers.Wrapper>> missing = new Dictionary<Wrappers.Group, List<Wrappers.Wrapper>>();
			
			d_metaNetwork.Merge(d_network);
			Merge(d_network, d_metaNetwork, missing);
			
			Dictionary<Wrappers.Group, List<Wrappers.Wrapper>> missingTemplates = new Dictionary<Wrappers.Group, List<Wrappers.Wrapper>>();
			
			d_metaTemplates.Merge(d_network.TemplateGroup);
			Merge(d_network.TemplateGroup, d_metaTemplates, missingTemplates);
			
			d_metaFunctions.Merge(d_network.FunctionGroup);
			Merge(d_network.FunctionGroup, d_metaFunctions);
			
			// Now do some layouting on the missing guys?
			foreach (KeyValuePair<Wrappers.Group, List<Wrappers.Wrapper>> pair in missing)
			{
				foreach (Wrappers.Wrapper wrapper in pair.Value)
				{
					if (wrapper.WrappedObject.SupportsLocation())
					{
						int x;
						int y;

						wrapper.WrappedObject.GetLocation(out x, out y);
						wrapper.Allocation.X = x;
						wrapper.Allocation.Y = y;
					}
				}
			}
		}
		
		private string GenerateProjectFilename(string filename)
		{
			return "." + Path.GetFileName(filename) + "-project";
		}
		
		public void Load(string filename)
		{
			d_filename = filename;

			XmlDocument doc = new XmlDocument();
			XmlNode projectNode = null;
			
			try
			{
				doc.Load(filename);
			}
			catch
			{
				doc = null;
			}
			
			if (doc != null)
			{
				projectNode = doc.SelectSingleNode("/cpg/project");
				d_cansave = true;
			}
			else
			{
				d_cansave = false;
			}
			
			XmlAttribute at = projectNode != null ? projectNode.Attributes["path"] : null;
			XmlAttribute shared = projectNode != null ? projectNode.Attributes["shared"] : null;
			
			d_shared = (shared != null && shared.InnerText.Trim() == "yes");
			
			if (projectNode != null)
			{
				/* Remove project node from doc, then load network from XML to prevent
				   it from saving the project node as an external data */
				projectNode.ParentNode.RemoveChild(projectNode);
				
				StringWriter swriter = new StringWriter();
				XmlWriter xmlWriter = XmlTextWriter.Create(swriter, WriterSettings());
				
				doc.Save(xmlWriter);
				d_network.LoadFromXml(swriter.ToString());
				
				d_saveProjectExternally = false;
			}
			else
			{
				d_network.LoadFromPath(filename);
			}

			if (projectNode == null || at != null)
			{
				// Try to load external project doc
				string extfile;
				
				if (at == null)
				{
					d_externalPath = GenerateProjectFilename(filename);
				}
				else
				{
					d_externalPath = at.InnerText.Trim();
				}

				string path = d_externalPath;
					
				if (!Path.IsPathRooted(path))
				{
					extfile = Path.Combine(Path.GetDirectoryName(filename), path);
				}
				else
				{
					extfile = path;
				}
				
				if (File.Exists(extfile))
				{
					doc.Load(extfile);
					projectNode = doc.SelectSingleNode("/cpg/project");
					d_externalProjectFile = extfile;
					
					d_saveProjectExternally = true;
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
			XmlNode settings = node.SelectSingleNode("settings");
			
			if (settings != null)
			{
				d_settings = Deserialize<Project.SettingsType>(settings);
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
			
			// Extract the function metadata
			XmlNode func = node.SelectSingleNode("functions");
			
			if (func != null)
			{
				d_metaFunctions = Deserialize<Functions>(func);
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
			foreach (Wrappers.Wrapper child in orig.Children)
			{
				Type t = TypeMap(child.GetType());
				
				if (t == null)
				{
					continue;
				}
				
				Object obj = (Object)t.GetConstructor(new Type[] {}).Invoke(new object[] {});
				obj.Transfer(child);
				
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
			d_metaNetwork.Transfer(d_network);
			CreateMeta(d_network, d_metaNetwork);
			
			d_metaTemplates = new Templates();
			d_metaTemplates.Transfer(d_network.TemplateGroup);
			CreateMeta(d_network.TemplateGroup, d_metaTemplates);
			
			d_metaFunctions = new Functions();
			d_metaFunctions.Transfer(d_network.FunctionGroup);
			CreateMeta(d_network.FunctionGroup, d_metaFunctions);
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
			XmlNode settingsNode = Serialize(d_settings);
			XmlNode functionsNode = Serialize(d_metaFunctions);
			
			if (SaveProjectExternally)
			{
				if (d_externalPath == null || (!d_shared && filename != d_filename))
				{
					d_externalPath = GenerateProjectFilename(filename);
				}

				// Write to separate files
				string xml = serializer.SerializeMemory();
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);
				
				// Append a project xml which imports
				XmlNode root = doc.SelectSingleNode("cpg");
				XmlElement projectNode = doc.CreateElement("project");
				projectNode.SetAttribute("path", d_externalPath);
				
				if (d_shared)
				{
					projectNode.SetAttribute("shared", "yes");
				}

				root.AppendChild(projectNode);

				Save(doc, filename);
				
				string extfile = Path.Combine(Path.GetDirectoryName(filename), d_externalPath);;
				
				XmlDocument ext = new XmlDocument();
				root = ext.CreateElement("cpg");
				ext.AppendChild(root);
				
				projectNode = ext.CreateElement("project");
				
				projectNode.AppendChild(ext.ImportNode(settingsNode, true));
				projectNode.AppendChild(ext.ImportNode(networkNode, true));
				projectNode.AppendChild(ext.ImportNode(templatesNode, true));
				projectNode.AppendChild(ext.ImportNode(functionsNode, true));
				
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

				projectNode.AppendChild(doc.ImportNode(settingsNode, true));
				projectNode.AppendChild(doc.ImportNode(networkNode, true));
				projectNode.AppendChild(doc.ImportNode(templatesNode, true));
				projectNode.AppendChild(doc.ImportNode(functionsNode, true));

				root.AppendChild(projectNode);
				Save(doc, filename);
			}
			
			d_filename = filename;
			d_cansave = true;
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

