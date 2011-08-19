using System;

namespace Cpg.Studio
{
	public class Simulation
	{
		private Wrappers.Network d_network;
		private Integrator d_integrator;
		private Range d_range;
		
		public event BeginHandler OnBegin = delegate {};
		public event EventHandler OnEnd = delegate {};
		private event SteppedHandler OnSteppedProxy;
		
		private bool d_running;
		private uint d_idleRun;

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
			
			UpdateIntegrator();
			
			d_network.WrappedObject.AddNotification("integrator", HandleNotifyIntegrator);
		}
		
		public Range Range
		{
			get
			{
				return d_range;
			}
		}
		
		private void HandleNotifyIntegrator(object source, GLib.NotifyArgs args)
		{
			UpdateIntegrator();
		}
		
		private void UpdateIntegrator()
		{
			bool hasSteppers = OnSteppedProxy != null && OnSteppedProxy.GetInvocationList().Length != 0;
			
			if (d_integrator != null)
			{
				if (hasSteppers)
				{
					d_integrator.Stepped -= HandleIntegratorStepped;
				}

				d_integrator.Begin -= HandleIntegratorBegin;
				d_integrator.End -= HandleIntegratorEnd;
			}
			
			d_integrator = d_network.Integrator;
			
			if (d_integrator != null)
			{
				d_integrator.Begin += HandleIntegratorBegin;
				d_integrator.End += HandleIntegratorEnd;
				
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
		
		private void HandleIntegratorBegin(object source, BeginArgs args)
		{
			d_range = new Range(args.From, args.Step, args.To);
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
			d_network.Run(from, timestep, to);
		}
		
		public Cpg.Network Network
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
