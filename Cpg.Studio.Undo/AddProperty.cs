using System;

namespace Cpg.Studio.Undo
{
	public class AddProperty : Property, IAction
	{
		public AddProperty(Wrappers.Wrapper wrapped, string name, string expression, Cpg.PropertyFlags flags) : base(wrapped, name, expression, flags)
		{
		}
		
		public AddProperty(Wrappers.Wrapper wrapped, Cpg.Property prop) : this(wrapped, prop.Name, prop.Expression.AsString, prop.Flags)
		{
		}
		
		public string Description
		{
			get
			{
				return String.Format("Add property `{0}' in `{1}'", Prop.Name, Prop.Object.FullIdForDisplay);
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

