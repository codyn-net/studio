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
		
		public void MergeFromPath(string filename)
		{
			WrappedObject.MergeFromPath(filename);
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
		
		public Group TemplateGroup
		{
			get
			{
				return (Group)Wrap(WrappedObject.TemplateGroup);
			}
		}

		public Cpg.Function[] Functions
		{
			get
			{
				Cpg.Object[] children = WrappedObject.FunctionGroup.Children;
				Cpg.Function[] ret = new Cpg.Function[children.Length];

				for (int i = 0; i < children.Length; ++i)
				{
					ret[i] = (Cpg.Function)children[i];
				}
				
				return ret;
			}
		}
		
		public Cpg.Function GetFunction(string name)
		{
			return (Cpg.Function)WrappedObject.FunctionGroup.GetChild(name);
		}
		
		public Group FunctionGroup 
		{
			get
			{
				return (Group)Wrap(WrappedObject.FunctionGroup);
			}
		}
	}
}
