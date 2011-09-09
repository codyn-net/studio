using System;

namespace Cpg.Studio.Widgets
{
	public abstract class GenericFunctionNode : Node
	{
		private Wrappers.Function d_function;

		private void Disconnect()
		{
			if (d_function == null)
			{
				return;
			}

			d_function.WrappedObject.RemoveNotification("id", OnPropertyChanged);
			
			if (d_function.Expression != null)
			{
				d_function.Expression.RemoveNotification("expression", OnPropertyChanged);
			}

			d_function.WrappedObject.ArgumentAdded -= OnArgumentsChanged;
			d_function.WrappedObject.ArgumentRemoved -= OnArgumentsChanged;
		}
		
		private void Connect()
		{
			if (d_function == null)
			{
				return;
			}

			d_function.WrappedObject.AddNotification("id", OnPropertyChanged);
			
			if (d_function.Expression != null)
			{
				d_function.Expression.AddNotification("expression", OnPropertyChanged);
			}

			d_function.WrappedObject.ArgumentAdded += OnArgumentsChanged;
			d_function.WrappedObject.ArgumentRemoved += OnArgumentsChanged;
		}

		[PrimaryKey]
		public Wrappers.Function Function
		{
			get
			{
				return d_function;
			}
			set
			{
				Disconnect();
				d_function = value;
				Connect();
			}
		}
		
		~GenericFunctionNode()
		{
			Disconnect();
		}
		
		private void OnPropertyChanged(object source, GLib.NotifyArgs args)
		{
			EmitChanged();
		}
		
		private void OnArgumentsChanged(object source, EventArgs args)
		{
			EmitChanged();
		}

	}
}

