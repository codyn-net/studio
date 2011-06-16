using System;

namespace Cpg.Studio.Wrappers
{
	public class Network : Group
	{
		protected Network(Cpg.Network obj) : base (obj)
		{
		}
		
		public Network() : this(new Cpg.Network())
		{
		}
		
		public Network(string filename) : this(new Cpg.Network(filename))
		{
		}
		
		public static Network NewFromString(string s)
		{
			return new Network(Cpg.Network.NewFromString(s));
		}
		
		public new Cpg.Network WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Network;
			}
		}
		
		public string Path
		{
			get
			{
				return WrappedObject.Path;
			}
		}
		
		public Wrappers.Import GetImportFromPath(string path)
		{
			return (Wrappers.Import)Wrappers.Wrapper.Wrap(WrappedObject.GetImportFromPath(path));
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
		
		public bool LoadFromPath(string filename)
		{
			return WrappedObject.LoadFromPath(filename);
		}
		
		public bool LoadFromString(string s)
		{
			return WrappedObject.LoadFromString(s);
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
		
		public void MergeFromString(string s)
		{
			WrappedObject.MergeFromString(s);
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

		public Wrappers.Function[] Functions
		{
			get
			{
				Cpg.Object[] children = WrappedObject.FunctionGroup.Children;
				Wrappers.Function[] ret = new Wrappers.Function[children.Length];

				for (int i = 0; i < children.Length; ++i)
				{
					ret[i] = (Wrappers.Function)Wrappers.Wrapper.Wrap(children[i]);
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
