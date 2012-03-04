using System;

namespace Cdn.Studio.Undo
{
	public class AddProperty : Property, IAction
	{
		public AddProperty(Wrappers.Wrapper wrapped, string name, string expression, Cdn.PropertyFlags flags) : base(wrapped, name, expression, flags)
		{
		}
		
		public AddProperty(Wrappers.Wrapper wrapped, Cdn.Property prop) : this(wrapped, prop.Name, prop.Expression.AsString, prop.Flags)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add property `{0}' in `{1}'", Name, Wrapped.FullId);
			}
		}
		
		public void Undo()
		{
			Remove();
		}
		
		public void Redo()
		{
			Add();
		}
	}
}

