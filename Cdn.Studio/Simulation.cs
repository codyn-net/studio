using System;

namespace Cdn.Studio
{
	public class Simulation
	{
		private Wrappers.Network d_network;
		private Integrator d_integrator;
		private SimulationRange d_range;
		
		public event Cdn.BegunHandler OnBegin = delegate {};
		public event EventHandler OnEnd = delegate {};
		private event SteppedHandler OnSteppedProxy;
		
		private bool d_running;
		private uint d_idleRun;

		private bool d_reseed;
		
		public event SteppedHandler OnStepped
		{
			add
			{
				OnSteppedProxy += value;
				
				if (OnSteppedProxy.GetInvocationList().Length == 1 && d_integrator != null)
				{
					d_integrator.Stepped += HandleIntegratorStepped;
				}
			}
			remove
			{
				OnSteppedProxy -= value;
				
				if (OnSteppedProxy.GetInvocationList().Length == 0 && d_integrator != null)
				{
					d_integrator.Stepped -= HandleIntegratorStepped;
				}
			}
		}

		public Simulation(Wrappers.Network network)
		{
			d_network = network;
			
			UpdateIntegrator(d_network.Integrator);

			d_network.Reverting += delegate {
				d_network.WrappedObject.RemoveNotification("integrator", HandleNotifyIntegrator);

				UpdateIntegrator(null);
			};

			d_network.WrappedObject.AddNotification("integrator", HandleNotifyIntegrator);

			d_network.WrappedObjectChanged += (source, oldwrapped) => {
				if (oldwrapped != null)
				{
					oldwrapped.RemoveNotification("integrator", HandleNotifyIntegrator);
				}

				d_network.WrappedObject.AddNotification("integrator", HandleNotifyIntegrator);

				UpdateIntegrator(d_network.Integrator);
			};
		}

		public bool Reseed
		{
			get { return d_reseed; }
			set { d_reseed = value; }
		}
		
		public SimulationRange Range
		{
			get
			{
				return d_range;
			}
			set
			{
				d_range = value;
			}
		}
		
		private void HandleNotifyIntegrator(object source, GLib.NotifyArgs args)
		{
			UpdateIntegrator(d_network.Integrator);
		}
		
		private void UpdateIntegrator(Cdn.Integrator integrator)
		{
			bool hasSteppers = OnSteppedProxy != null && OnSteppedProxy.GetInvocationList().Length != 0;
			
			if (d_integrator != null)
			{
				if (hasSteppers)
				{
					d_integrator.Stepped -= HandleIntegratorStepped;
				}

				d_integrator.Begun -= HandleIntegratorBegin;
				d_integrator.Ended -= HandleIntegratorEnd;
			}
			
			d_integrator = integrator;
			
			if (d_integrator != null)
			{
				d_integrator.Begun += HandleIntegratorBegin;
				d_integrator.Ended += HandleIntegratorEnd;
				
				if (hasSteppers)
				{
					d_integrator.Stepped += HandleIntegratorStepped;
				}
			}
		}
		
		public bool Running
		{
			get
			{
				return d_running;
			}
		}
		
		private void HandleIntegratorBegin(object source, BegunArgs args)
		{
			d_running = true;
			OnBegin(this, args);
		}
		
		private void HandleIntegratorEnd(object source, EventArgs args)
		{
			d_running = false;
			OnEnd(this, args);
		}
		
		private void HandleIntegratorStepped(object source, SteppedArgs args)
		{
			OnSteppedProxy(this, args);
		}

		public void Step(double timestep)
		{
			d_network.Step(timestep);
		}
		
		public void RunPeriod(double from, double timestep, double to)
		{
			if (d_reseed)
			{
				d_network.WrappedObject.RandomSeed = (uint)DateTime.Now.TimeOfDay.TotalMilliseconds;
			}

			d_network.Reset();

			try
			{
				d_network.Run(from, timestep, to);
			}
			catch
			{
			}
		}
		
		public Cdn.Network Network
		{
			get
			{
				return d_network;
			}
		}
		
		public void Reset()
		{
			d_network.Reset();
		}
		
		public void Resimulate()
		{
			Resimulate(false);
		}
		
		private bool OnIdleResimulate()
		{
			Resimulate(true);
			return false;
		}
		
		public void Resimulate(bool rightnow)
		{
			if (d_range != null)
			{
				if (rightnow && d_idleRun != 0)
				{
					GLib.Source.Remove(d_idleRun);
					d_idleRun = 0;
				}
				else if (!rightnow && d_idleRun == 0)
				{
					d_idleRun = GLib.Idle.Add(OnIdleResimulate);
				}
				
				if (rightnow)
				{
					RunPeriod(d_range.From, d_range.Step, d_range.To);
				}
			}
		}
	}
}
