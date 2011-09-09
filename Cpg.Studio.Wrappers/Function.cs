using System;

namespace Cpg.Studio.Wrappers
{
	public class Function : Object
	{
		public delegate void ArgumentHandler(Function source, Cpg.FunctionArgument argument);
		public delegate void ArgumentsReorderedHandler(Function source);

		public event ArgumentHandler ArgumentAdded = delegate {};
		public event ArgumentHandler ArgumentRemoved = delegate {};
		public event ArgumentsReorderedHandler ArgumentsReordered = delegate {}; 

		protected Function(Cpg.Function obj) : base(obj)
		{
			Renderer = new Renderers.Function(this);
		}
		
		public Function() : this(new Cpg.Function("f", "0"))
		{
		}
		
		public Function(string name, string expression) : this(new Cpg.Function(name, expression))
		{
		}
		
		protected override void ConnectWrapped()
		{
			base.ConnectWrapped();
			
			WrappedObject.ArgumentAdded += HandleArgumentAdded;
			WrappedObject.ArgumentRemoved += HandleArgumentRemoved;
			WrappedObject.ArgumentsReordered += HandleArgumentsReordered;
		}
		
		protected override void DisconnectWrapped()
		{
			base.DisconnectWrapped();

			WrappedObject.ArgumentAdded -= HandleArgumentAdded;
			WrappedObject.ArgumentRemoved -= HandleArgumentRemoved;
			WrappedObject.ArgumentsReordered -= HandleArgumentsReordered;
		}
		
		public new Cpg.Function WrappedObject
		{
			get
			{
				return base.WrappedObject as Cpg.Function;
			}
		}
		
		public static implicit operator Cpg.Function(Wrappers.Function obj)
		{
			return obj.WrappedObject;
		}
		
		public static implicit operator Function(Cpg.Function obj)
		{
			if (obj == null)
			{
				return null;
			}

			return (Function)Wrap(obj);
		}
		
		public FunctionArgument[] Arguments
		{
			get
			{
				return WrappedObject.Arguments;
			}
			set
			{
				ClearArguments();
				
				foreach (FunctionArgument arg in value)
				{
					AddArgument(arg);
				}
			}
		}
		
		public Cpg.Expression Expression
		{
			get
			{
				return WrappedObject.Expression;
			}
		}
		
		public void AddArgument(FunctionArgument arg)
		{
			WrappedObject.AddArgument(arg);
		}
		
		public void RemoveArgument(FunctionArgument arg)
		{
			WrappedObject.RemoveArgument(arg);
		}
		
		public void ClearArguments()
		{
			WrappedObject.ClearArguments();
		}
		
		private void HandleArgumentAdded(object o, Cpg.ArgumentAddedArgs args)
		{
			ArgumentAdded(this, args.Argument);
		}
		
		private void HandleArgumentRemoved(object o, Cpg.ArgumentRemovedArgs args)
		{
			ArgumentRemoved(this, args.Argument);
		}
		
		private void HandleArgumentsReordered(object o, EventArgs args)
		{
			ArgumentsReordered(this);
		}
	}
}
