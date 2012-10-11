using System;

namespace Cdn.Studio.Wrappers
{
	public class Network : Node
	{
		public event EventHandler Reverting = delegate {};
		public event EventHandler Reverted = delegate {};

		protected Network(Cdn.Network obj) : base (obj)
		{
		}
		
		public Network() : this(new Cdn.Network())
		{
		}
		
		public Network(string filename) : this(new Cdn.Network(filename))
		{
		}
		
		public static Network NewFromString(string s)
		{
			return new Network(Cdn.Network.NewFromString(s));
		}
		
		public new Cdn.Network WrappedObject
		{
			get
			{
				return base.WrappedObject as Cdn.Network;
			}
		}

		public void Revert()
		{
			Reverting(this, new EventArgs());
			SetWrappedObject(new Cdn.Network());
			Reverted(this, new EventArgs());
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
		
		public static implicit operator Cdn.Network(Network obj)
		{
			return obj.WrappedObject;
		}
		
		public Cdn.Integrator Integrator
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
		
		public void Merge(Cdn.Network other)
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
		
		public Node TemplateNode
		{
			get
			{
				return (Node)Wrap(WrappedObject.TemplateNode);
			}
		}
	}
}
