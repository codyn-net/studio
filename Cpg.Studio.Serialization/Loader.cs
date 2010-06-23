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

		private void Reconstruct(Cpg cpg, Serialization.Group group, List<CCpg.Object> all, Dictionary<string, Wrappers.Wrapper> mapping)
		{
			CCpg.Network network = cpg.Network.CNetwork;
			List<Serialization.Group> groups = new List<Serialization.Group>();
			List<Serialization.Link> links = new List<Serialization.Link>();
			
			group.Transfer();
		
			// Fill in C objects
			List<Serialization.Object> cp = new List<Serialization.Object>(group.Children);
			foreach (Serialization.Object obj in cp)
			{
				if (!(obj.Obj is Wrappers.Wrapper))
					continue;

				if (obj is Serialization.Group)
				{
					groups.Add(obj as Serialization.Group);
				}
				else
				{
					string id = obj.Id;
					Wrappers.Wrapper sim = obj.Obj as Wrappers.Wrapper;
					
					CCpg.Object cobj = network.GetObject(id);
					
					if (cobj == null)
					{
						group.Children.Remove(obj);
						continue;
					}
					
					all.Remove(cobj);
					
					// Set CObject
					sim.Object = cobj;
					
					if (obj is Serialization.Link)
						links.Add(obj as Serialization.Link);
				}
			}

			// Reconstruct groups within before setting up links
			foreach (Serialization.Group g in groups)
			{
				Reconstruct(cpg, g, all, mapping);
			}
			
			foreach (Serialization.Object obj in group.Children)
			{
				if (!(obj.Obj is Wrappers.Wrapper))
					continue;

				Wrappers.Wrapper sim = obj.Obj as Wrappers.Wrapper;
				
				// Fill main of current group
				if (sim.FullId == group.Main)
				{
					(group.Obj as Wrappers.Group).Main = sim;
				}
					
				// Make mapping for links
				if (sim is Wrappers.Group)
				{
					mapping[(sim as Wrappers.Group).Main.FullId] = sim;
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
				Wrappers.Link wrapped = link.As<Wrappers.Link>();
				CCpg.Link clink = wrapped.Object as CCpg.Link;
				
				if (!mapping.ContainsKey(clink.To.Id))
				{
					Console.WriteLine("Not found to: " + clink.To.Id);

					// Apparently, there is no wrapper for this, so... try to just construct it here
					Wrappers.Wrapper sim = Wrappers.Wrapper.FromCpg(clink.To);
					mapping[clink.To.Id] = sim;
					
					group.Children.Add(Serialization.Object.Create(sim));
					all.Remove(clink.To);
				}
				
				if (!mapping.ContainsKey(clink.From.Id))
				{
					Console.WriteLine("Not found from: " + clink.From.Id);
					
					// Apparently, there is no wrapper for this, so... try to just construct it here
					Wrappers.Wrapper sim = Wrappers.Wrapper.FromCpg(clink.From);
					mapping[clink.From.Id] = sim;
					
					group.Children.Add(Serialization.Object.Create(sim));
					all.Remove(clink.From);
				}
				
				wrapped.To = mapping[clink.To.Id];
				wrapped.From = mapping[clink.From.Id];
			}
		}
		
		private void Rewrap(Serialization.Group root, List<CCpg.Object> all, Dictionary<string, Wrappers.Wrapper> mapping)
		{
			List<Wrappers.Wrapper> states = new List<Wrappers.Wrapper>();
			List<CCpg.Object> cp = new List<CCpg.Object>(all);
			
			foreach (CCpg.Object obj in cp)
			{
				if (!(obj is CCpg.Link))
					continue;

				CCpg.Link link = obj as CCpg.Link;
				
				if (!mapping.ContainsKey(link.To.Id))
				{
					Console.WriteLine("Not found to: " + link.To.Id);
					// So, we need to create it...
					Wrappers.Wrapper s = Wrappers.Wrapper.FromCpg(link.To);
					mapping[link.To.Id] = s;
					
					root.Children.Add(Serialization.Object.Create(s));

					all.Remove(link.To);
					states.Add(s);
				}
				
				if (!mapping.ContainsKey(link.From.Id))
				{
					Console.WriteLine("Not found from: " + link.From.Id);
					
					// So, we need to create it
					Wrappers.Wrapper s = Wrappers.Wrapper.FromCpg(link.From);
					mapping[link.From.Id] = s;
					
					root.Children.Add(Serialization.Object.Create(s));

					all.Remove(link.From);
					states.Add(s);
				}

				Wrappers.Wrapper sim = Wrappers.Wrapper.FromCpg(obj);
				(sim as Wrappers.Link).From = mapping[link.From.Id];
				(sim as Wrappers.Link).To = mapping[link.To.Id];
				
				root.Children.Add(Serialization.Object.Create(sim));
				all.Remove(obj);
			}

			foreach (CCpg.Object obj in all)
			{
				Wrappers.Wrapper sim = Wrappers.Wrapper.FromCpg(obj);
				mapping[obj.Id] = sim;

				states.Add(sim);
				root.Children.Add(Serialization.Object.Create(sim));
			}

			// Make grid of objects at 0, 0 with aspect ratio about 4x3, spacing 4
			int spacing = 4;
			double ratio = 4 / 3;
			
			int columns = (int)Math.Ceiling(Math.Sqrt(ratio * states.Count));
			
			for (int i = 0; i < states.Count; ++i)
			{
				int col = i % columns * (spacing + 1);
				int row = i / columns * (spacing + 1);
				
				states[i].Allocation = new Allocation(col, row, 1, 1);
			}
		}
		
		private void Reconstruct(Cpg cpg)
		{
			// Reconstruct wrapped network
			List<CCpg.Object> all = new List<CCpg.Object>();
			
			foreach (CCpg.Object obj in cpg.Network.CNetwork.States)
			{
				all.Add(obj);
			}
			
			foreach (CCpg.Link link in cpg.Network.CNetwork.Links)
			{
				all.Add(link);
			}
			
			Dictionary<string, Wrappers.Wrapper> mapping = new Dictionary<string, Wrappers.Wrapper>();
			Reconstruct(cpg, cpg.Project.Root, all, mapping);
			Rewrap(cpg.Project.Root, all, mapping);
		}
		
		public Cpg LoadXml(string xml)
		{
			/* Load in the network */
			CCpg.Network network = CCpg.Network.NewFromXml(xml);
			
			if (network == null || network.Handle == IntPtr.Zero)
			{
				throw new Exception("Could not construct network. This usually means that the network file is corrupt.");
			}
			
			/* Load in project, which builds the wrapper network */
			StringReader stream = new StringReader(xml);
			XmlDocument doc = new XmlDocument();
			doc.Load(stream);
			stream.Close();
			
			XmlNode project = doc.SelectSingleNode("/cpg/project");
			object des = null;
			
			if (project != null)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Project));
				
				XmlReader r = new XmlNodeReader(project);
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.IgnoreComments = true;
				settings.IgnoreWhitespace = true;
				
				XmlReader reader = XmlReader.Create(r, settings);
				des = serializer.Deserialize(reader);
				reader.Close();
				
				if (des == null)
				{
					throw new Exception("Could not construct project");
				}
			}
			
			Cpg cpg = new Cpg(network);
			
			if (des != null)
			{
				cpg.Project = des as Project;
			}
			else
			{
				cpg.Project = new Project();
				cpg.Project.Root = new Serialization.Group();
			}
			
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
