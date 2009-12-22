using System;

namespace Cpg.Studio
{
	public class Simulation
	{
		public struct Args
		{
			public double From;
			public double Step;
			public double To;
		}
		
		Components.Network d_network;
		Range d_range;
		bool d_inPeriod;
		
		public delegate void SimulationRunHandler(object source, Args args);
		public delegate void SimulationStepHandler(object source, double timestep);

		public event SimulationStepHandler OnStep = delegate {};
		public event SimulationRunHandler OnPeriodBegin = delegate {};
		public event SimulationRunHandler OnPeriodEnd = delegate {};
		
		public Simulation(Components.Network network)
		{
			d_network = network;
			d_range = null;
			
			/* TODO: proxy step
			d_network.Updated += delegate(object o, UpdatedArgs args) {
				OnStep(this, args.Timestep);
			};*/
		}
		
		public void Step(double timestep)
		{
			d_range = null;
			d_network.Step(timestep);
			OnStep(this, timestep);
		}
		
		public void RunPeriod(double from, double timestep, double to)
		{
			Args args;
			
			args.From = from;
			args.Step = timestep;
			args.To = to;
			
			d_range = new Range(from, timestep, to);
			d_inPeriod = true;
			
			d_network.Reset();

			OnPeriodBegin(this, args);
			
			d_network.Run(from, timestep, to);
			
			OnPeriodEnd(this, args);
			
			d_inPeriod = false;
		}
		
		public Cpg.Network Network
		{
			get
			{
				return d_network;
			}
		}
		
		public Range Range
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
		
		public bool InPeriod
		{
			get
			{
				return d_inPeriod;
			}
		}
		
		public void Reset()
		{
			d_network.Reset();
		}
		
		public void Resimulate()
		{
			if (d_range != null)
			{
				RunPeriod(d_range.From, d_range.Step, d_range.To);
			}
		}
	}
}
