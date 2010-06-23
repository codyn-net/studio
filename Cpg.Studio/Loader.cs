using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Reflection;

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

		private void Reconstruct(Main cpg, Serialization.Group group, List<Cpg.Object> all, Dictionary<string, Wrappers.Wrapper> mapping)
		{
			Cpg.Network network = cpg.Network.CNetwork;
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
					//Wrappers.Wrapper sim = obj.Obj as Wrappers.Wrapper;
					
					Cpg.Object cobj = network.GetChild(id);
					
					if (cobj == null)
					{
						// TODO
						//group.Remove(obj);
						continue;
					}
					
					all.Remove(cobj);
					
					// Set CObject
					// TODO
					//sim.Object = cobj;
					
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

				//Wrappers.Wrapper sim = obj.Obj as Wrappers.Wrapper;
				
				// Fill main of current group
				/*TODO
				if (sim.Id == group.Main)
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
				}*/
			}
						
			// Set link objects
			foreach (Serialization.Link link in links)
			{
				// Fill in wrapped to/from, use earlier constructed mapping
				Wrappers.Link wrapped = link.As<Wrappers.Link>();
				Cpg.Link clink = wrapped.WrappedObject;
				
				if (!mapping.ContainsKey(clink.To.Id))
				{
					Console.WriteLine("Not found to: " + clink.To.Id);

					// Apparently, there is no wrapper for this, so... try to just construct it here
					Wrappers.Wrapper sim = Wrappers.Wrapper.Wrap(clink.To);
					mapping[clink.To.Id] = sim;
					
					group.Children.Add(Serialization.Object.Create(sim));
					all.Remove(clink.To);
				}
				
				if (!mapping.ContainsKey(clink.From.Id))
				{
					Console.WriteLine("Not found from: " + clink.From.Id);
					
					// Apparently, there is no wrapper for this, so... try to just construct it here
					Wrappers.Wrapper sim = Wrappers.Wrapper.Wrap(clink.From);
					mapping[clink.From.Id] = sim;
					
					group.Children.Add(Serialization.Object.Create(sim));
					all.Remove(clink.From);
				}
				
				wrapped.To = mapping[clink.To.Id];
				wrapped.From = mapping[clink.From.Id];
			}
		}
		
		private void Rewrap(Serialization.Group root, List<Cpg.Object> all, Dictionary<string, Wrappers.Wrapper> mapping)
		{
			List<Wrappers.Wrapper> states = new List<Wrappers.Wrapper>();
			List<Cpg.Object> cp = new List<Cpg.Object>(all);
			
			foreach (Cpg.Object obj in cp)
			{
				if (!(obj is Cpg.Link))
					continue;

				Cpg.Link link = obj as Cpg.Link;
				
				if (!mapping.ContainsKey(link.To.Id))
				{
					Console.WriteLine("Not found to: " + link.To.Id);
					// So, we need to create it...
					Wrappers.Wrapper s = Wrappers.Wrapper.Wrap(link.To);
					mapping[link.To.Id] = s;
					
					root.Children.Add(Serialization.Object.Create(s));

					all.Remove(link.To);
					states.Add(s);
				}
				
				if (!mapping.ContainsKey(link.From.Id))
				{
					Console.WriteLine("Not found from: " + link.From.Id);
					
					// So, we need to create it
					Wrappers.Wrapper s = Wrappers.Wrapper.Wrap(link.From);
					mapping[link.From.Id] = s;
					
					root.Children.Add(Serialization.Object.Create(s));

					all.Remove(link.From);
					states.Add(s);
				}

				Wrappers.Wrapper sim = Wrappers.Wrapper.Wrap(obj);
				(sim as Wrappers.Link).From = mapping[link.From.Id];
				(sim as Wrappers.Link).To = mapping[link.To.Id];
				
				root.Children.Add(Serialization.Object.Create(sim));
				all.Remove(obj);
			}

			foreach (Cpg.Object obj in all)
			{
				Wrappers.Wrapper sim = Wrappers.Wrapper.Wrap(obj);
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
		
		private void Reconstruct(Main cpg)
		{
			// Reconstruct wrapped network
			List<Cpg.Object> all = new List<Cpg.Object>();
			
			foreach (Cpg.Object obj in cpg.Network.CNetwork.Children)
			{
				all.Add(obj);
			}
			
			Dictionary<string, Wrappers.Wrapper> mapping = new Dictionary<string, Wrappers.Wrapper>();
			Reconstruct(cpg, cpg.Project.Root, all, mapping);
			Rewrap(cpg.Project.Root, all, mapping);
		}
		
		public Main LoadXml(string xml)
		{
			/* Load in the network */
			Cpg.Network network = Cpg.Network.NewFromXml(xml);
			
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
			
			Main cpg = new Main(network);
			
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
		
		public Main Load(string filename)
		{
			FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			TextReader reader = new StreamReader(stream, Encoding.UTF8);
			
			Main cpg = LoadXml(reader.ReadToEnd());
			reader.Close();
			
			return cpg;
		}
	}
}
