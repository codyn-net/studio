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
		
		public delegate void SimulationRunHandler(object source, Args args);
		public delegate void SimulationStepHandler(object source, double timestep);

		public event SimulationStepHandler OnStep = delegate {};
		public event SimulationRunHandler OnPeriodBegin = delegate {};
		public event SimulationRunHandler OnPeriodEnd = delegate {};
		
		public Simulation(Components.Network network)
		{
			d_network = network;
		}
		
		public void Step(double timestep)
		{
			d_network.Step(timestep);
			OnStep(this, timestep);
		}
		
		public void RunPeriod(double from, double timestep, double to)
		{
			Args args;
			
			args.From = from;
			args.Step = timestep;
			args.To = to;
			
			OnPeriodBegin(this, args);
			
			d_network.Run(from, timestep, to);
			
			OnPeriodEnd(this, args);
		}
	}
}
