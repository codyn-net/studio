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
	public class Loader
	{
		public class Exception : System.Exception
		{
			public Exception(string message) : base(message)
			{
			}
		}

		private void Reconstruct(Cpg cpg, Serialization.Group group)
		{
			CCpg.Network network = cpg.Network.CNetwork;
			List<Serialization.Group> groups = new List<Serialization.Group>();
			List<Serialization.Link> links = new List<Serialization.Link>();
			
			Dictionary<string, Components.Simulated> mapping = new Dictionary<string, Components.Simulated>();
			
			group.Transfer();
		
			// Fill in C objects
			foreach (Serialization.Object obj in group.Children)
			{
				if (!(obj.Obj is Components.Simulated))
					continue;

				if (obj is Serialization.Group)
				{
					groups.Add(obj as Serialization.Group);
				}
				else
				{
					string id = obj.Id;
					Components.Simulated sim = obj.Obj as Components.Simulated;
					
					CCpg.Object cobj = network.GetObject(id);
					
					if (cobj == null)
					{
						throw new Exception("Could not find object '" + id + "'");
					}
					
					// Set CObject
					sim.Object = cobj;
					
					if (obj is Serialization.Link)
						links.Add(obj as Serialization.Link);
				}
			}

			// Reconstruct groups within before setting up links
			foreach (Serialization.Group g in groups)
			{
				Reconstruct(cpg, g);
			}
			
			foreach (Serialization.Object obj in group.Children)
			{
				if (!(obj.Obj is Components.Simulated))
					continue;

				Components.Simulated sim = obj.Obj as Components.Simulated;
				
				// Fill main of current group
				if (sim.FullId == group.Main)
				{
					(group.Obj as Components.Group).Main = sim;
				}
					
				// Make mapping for links
				if (sim is Components.Group)
				{
					mapping[(sim as Components.Group).Main.FullId] = sim;
				}
				else
				{
					mapping[sim.FullId] = sim;
				}
			}
						
			// Set link objects
			foreach (Serialization.Link link in links)
			{
				// Fill in wrapped to/from, use earlier constructed mapping
				Components.Link wrapped = link.As<Components.Link>();
				CCpg.Link clink = wrapped.Object as CCpg.Link;
				
				if (!mapping.ContainsKey(clink.To.Id))
				{
					throw new Exception("Could not locate link target '" + clink.To.Id + "' for '" + wrapped.FullId + "'");
				}
				
				if (!mapping.ContainsKey(clink.From.Id))
				{
					throw new Exception("Could not locate link source '" + clink.From.Id + "' for '" + wrapped.FullId + "'");
				}
				
				wrapped.To = mapping[clink.To.Id];
				wrapped.From = mapping[clink.From.Id];
			}
		}
		
		private void Reconstruct(Cpg cpg)
		{
			Reconstruct(cpg, cpg.Project.Root);
		}
		
		public Cpg LoadXml(string xml)
		{
			/* Load in the network */
			CCpg.Network network = CCpg.Network.NewFromXml(xml);
			
			if (network == null || network.Handle == IntPtr.Zero)
			{
				throw new Exception("Could not construct network. This usually means that the network file is corrupted.");
			}
			
			/* Load in project, which builds the wrapper network */
			StringReader stream = new StringReader(xml);
			XmlDocument doc = new XmlDocument();
			doc.Load(stream);
			stream.Close();
			
			XmlNode project = doc.SelectSingleNode("/cpg/project");
			XmlSerializer serializer = new XmlSerializer(typeof(Project));
			
			XmlReader r = new XmlNodeReader(project);
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.IgnoreComments = true;
			settings.IgnoreWhitespace = true;
			
			XmlReader reader = XmlReader.Create(r, settings);
			object des = serializer.Deserialize(reader);
			reader.Close();			
			
			if (des == null)
			{
				throw new Exception("Could not construct project");
			}
			
			Cpg cpg = new Cpg(network);
			
			// Set network
			cpg.Network.CNetwork = network;
			cpg.Project = des as Project;
			
			// Reconstruct wrapped network
			Reconstruct(cpg);
			
			return cpg;
		}
		
		public Cpg Load(string filename)
		{
			FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			TextReader reader = new StreamReader(stream, Encoding.UTF8);
			
			Cpg cpg = LoadXml(reader.ReadToEnd());
			reader.Close();
			
			return cpg;
		}
	}
}