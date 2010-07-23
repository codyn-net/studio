using System;

namespace Cpg.Studio.Undo
{
	public class ModifyIntegrator : IAction
	{
		private Wrappers.Network d_network;
		private Cpg.Integrator d_prevIntegrator;
		private Cpg.Integrator d_integrator;

		public ModifyIntegrator(Wrappers.Network network, Cpg.Integrator integrator)
		{
			d_network = network;
			d_prevIntegrator = network.Integrator;
			d_integrator = integrator;
		}
		
		public void Undo()
		{
			d_network.Integrator = d_prevIntegrator;
		}
		
		public void Redo()
		{
			d_network.Integrator = d_integrator;
		}
		
		public bool Verify()
		{
			return true;
		}
		
		public bool CanMerge(IAction other)
		{
			return false;
		}
		
		public void Merge(IAction other)
		{
		}
	}
}

