using System;

namespace Cpg.Studio.Wrappers
{
	public class Network : Group
	{
		public Network(Cpg.Network obj) : base (obj)
		{
		}
		
		public Network() : this(new Cpg.Network())
		{
		}
		
		public Network(string filename) : this(new Cpg.Network(filename))
		{
		}
		
		public static Network NewFromXml(string xml)
		{
			return new Network(Cpg.Network.NewFromXml(xml));
		}
		
		public new Cpg.Network WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Network;
			}
		}
		
		public static implicit operator Cpg.Network(Network obj)
		{
			return obj.WrappedObject;
		}
		
		public Cpg.Integrator Integrator
		{
			get
			{
				return WrappedObject.Integrator;
			}
			set
			{
				WrappedObject.Integrator = value;
			}
		}
		
		public string WriteToXml()
		{
			return WrappedObject.WriteToXml();
		}
		
		public void WriteToFile(string filename)
		{
			WrappedObject.WriteToFile(filename);
		}
		
		public void Merge(Network other)
		{
			WrappedObject.Merge(other);
		}
		
		public void Merge(Cpg.Network other)
		{
			WrappedObject.Merge(other);
		}
		
		public void MergeFromFile(string filename)
		{
			WrappedObject.MergeFromFile(filename);
		}
		
		public void MergeFromXml(string xml)
		{
			WrappedObject.MergeFromXml(xml);
		}
		
		public void Run(double from, double timestep, double to)
		{
			WrappedObject.Run(from, timestep, to);
		}
		
		public void Step(double timestep)
		{
			WrappedObject.Step(timestep);
		}
		
		public Wrapper[] Templates
		{
			get
			{
				return Wrap(WrappedObject.Templates);
			}
		}
		
		public void RemoveTemplate(string name)
		{
			WrappedObject.RemoveTemplate(name);
		}
		
		public void RemoveTemplate(Wrapper obj)
		{
			RemoveTemplate(obj.Id);
		}
		
		public Wrapper AddFromTemplate(string name)
		{
			return Wrap(WrappedObject.AddFromTemplate(name));
		}
		
		public Wrapper AddLinkFromTemplate(string name, Wrapper from, Wrapper to)
		{
			return Wrap(WrappedObject.AddLinkFromTemplate(name, from, to));
		}
		
		public void AddFunction(Cpg.Function function)
		{
			WrappedObject.AddFunction(function);
		}
		
		public void RemoveFunction(Cpg.Function function)
		{
			WrappedObject.RemoveFunction(function);
		}
		
		public Cpg.Function[] Functions
		{
			get
			{
				return WrappedObject.Functions;
			}
		}
		
		public Cpg.Function GetFunction(string name)
		{
			return WrappedObject.GetFunction(name);
		}		
	}
}
