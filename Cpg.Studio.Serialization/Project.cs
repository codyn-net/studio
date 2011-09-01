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
		[XmlType("series")]
		public class Series
		{
			[XmlAttribute("y")]
			public string Y;

			[XmlAttribute("x")]
			public string X;
			
			[XmlAttribute("color")]
			public string Color;
		}

		[XmlType("monitor")]
		public class Monitor
		{
			[XmlElement("plots"),
			 XmlElement(typeof(Series))]
			public List<Series> Plots;
			
			[XmlAttribute("row")]
			public int Row;
			
			[XmlAttribute("column")]
			public int Column;
			
			public Monitor()
			{
				Plots = new List<Series>();
			}
			
			public Plot.Settings Settings { get; set; }
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
			
			[XmlElement("pane-position")]
			public int PanePosition;
			
			[XmlElement("side-bar-pane-position")]
			public int SideBarPanePosition;
			
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
				public int Rows;
				
				[XmlAttribute("columns")]
				public int Columns;
				
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
				PanePosition = 250;
				SideBarPanePosition = 250;
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
			
		public SettingsType Settings
		{
			get
			{
				return d_settings;
			}
		}
		
		public void Clear()
		{
			d_settings = new SettingsType();

			d_externalProjectFile = null;
			d_filename = null;
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
				d_network.LoadFromString(swriter.ToString());
				
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
					doc = new XmlDocument();
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
			
			AddRecent(filename);
		}
		
		private bool IsXml(string filename)
		{
			bool ret = false;
			FileStream fstr = new FileStream(filename, FileMode.Open);
			
			while (fstr.CanRead)
			{
				char c = (char)fstr.ReadByte();
				
				if (!char.IsWhiteSpace(c))
				{
					ret = c == '<';
					break;
				}
			}
			
			fstr.Close();
			return ret;
		}
		
		private void AddRecent(string filename)
		{
			Gtk.RecentData data = new Gtk.RecentData();
			
			data.AppName = "cpgstudio";
			
			if (IsXml(filename))
			{
				data.MimeType = "application/xml";
			}
			else
			{
				data.MimeType = "text/x-cpg";
			}

			data.AppExec = System.IO.Path.Combine(System.IO.Path.Combine(Config.Prefix, "bin"), "cpgstudio");

			Gtk.RecentManager.Default.AddFull("file://" + filename, data);
		}
		
		private void ExtractAnnotations(XmlNode node)
		{
			// Extract show settings
			XmlNode settings = node.SelectSingleNode("settings");
			
			if (settings != null)
			{
				d_settings = Deserialize<Project.SettingsType>(settings);
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
			AddRecent(d_filename);
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
		
		public void SaveProject()
		{
			if (d_externalPath == null)
			{
				d_externalPath = GenerateProjectFilename(d_filename);
			}
			
			SaveProjectExternally = true;
			
			SaveProject(Path.Combine(Path.GetDirectoryName(d_filename), d_externalPath));
		}
		
		public void SaveProject(string filename)
		{
			XmlNode settingsNode = Serialize(d_settings);
			
			XmlDocument ext = new XmlDocument();
			XmlNode root = ext.CreateElement("cpg");
			ext.AppendChild(root);
				
			XmlNode projectNode = ext.CreateElement("project");
			projectNode.AppendChild(ext.ImportNode(settingsNode, true));
				
			root.AppendChild(projectNode);
				
			ext.Save(filename);
		}
		
		public void Save(string filename)
		{
			Cpg.NetworkSerializer serializer = new Cpg.NetworkSerializer(d_network, d_network.WrappedObject);
			
			XmlNode settingsNode = Serialize(d_settings);
			
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

