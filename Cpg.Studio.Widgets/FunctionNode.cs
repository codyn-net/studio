using System;

namespace Cpg.Studio.Widgets
{
	public class FunctionNode : GenericFunctionNode
	{
		private FunctionArgument d_argument;
		
		private void Connect()
		{
			if (d_argument == null)
			{
				return;
			}
			
			d_argument.AddNotification("name", HandleArgumentChanged);
			d_argument.AddNotification("optional", HandleArgumentChanged);
			d_argument.AddNotification("default", HandleArgumentChanged);
		}
		
		private void HandleArgumentChanged(object source, GLib.NotifyArgs args)
		{
			EmitChanged();
		}
		
		private void Disconnect()
		{
			if (d_argument == null)
			{
				return;
			}
			
			d_argument.RemoveNotification("name", HandleArgumentChanged);
			d_argument.RemoveNotification("optional", HandleArgumentChanged);
			d_argument.RemoveNotification("default", HandleArgumentChanged);
		}

		public FunctionArgument Argument
		{
			get
			{
				return d_argument;
			}
			set
			{
				Disconnect();
				d_argument = value;
				Connect();
			}
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

