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
		
		public string Description
		{
			get
			{
				return String.Format("Change integrator from `{0}' to `{1}'", d_prevIntegrator.Name, d_integrator.Name);
			}
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

