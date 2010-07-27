using System;

namespace Cpg.Studio.Widgets
{
	public class FunctionNode : GenericFunctionNode
	{
		private void HandleArgumentChanged(object source, GLib.NotifyArgs args)
		{
			EmitChanged();
		}

		[NodeColumn(0)]
		public string Name
		{
			get
			{
				return Function.Id;
			}
		}
		
		[NodeColumn(1)]
		public string Arguments
		{
			get
			{
				string ret = "";

				foreach (Cpg.FunctionArgument arg in Function.Arguments)
				{
					if (ret != "")
					{
						ret += ", ";
					}
	
					ret += arg.Name;
					
					if (arg.Optional)
					{
						ret += " = " + arg.DefaultValue.ToString();
					}
				}
				
				return ret;
			}
		}
		
		[NodeColumn(2)]
		public string Expression
		{
			get
			{
				return Function.Expression.AsString;
			}
		}
		
		[PrimaryKey]
		public Cpg.Expression CpgExpression
		{
			get
			{
				return Function.Expression;
			}
		}
	}
}

