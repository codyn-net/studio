using System;
using Biorob.Math;

namespace Cpg.Studio.Undo
{
	public class Import : IAction
	{
		private Wrappers.Network d_network;
		private Wrappers.Group d_parent;
		private string d_id;
		private string d_filename;
		private Wrappers.Import d_import;

		public Import(Wrappers.Network network, Wrappers.Group parent, string id, string filename)
		{
			d_network = network;
			d_parent = parent;
			d_id = id;
			d_filename = filename;
		}
		
		public string Description
		{
			get
			{
				return String.Format("Import `{0}' as `{1}'",
				                     d_filename,
				                     d_id);
			}
		}
		
		private void MergeAnnotations(Wrappers.Wrapper original, Wrappers.Wrapper imported)
		{
			imported.Allocation = original.Allocation.Copy();
			
			Wrappers.Group grp = original as Wrappers.Group;
			Wrappers.Group imp = imported as Wrappers.Group;
			
			if (grp != null)
			{
				foreach (Wrappers.Wrapper wrapper in grp.Children)
				{
					Wrappers.Wrapper other = imp.GetChild(wrapper.Id);
					
					if (other != null)
					{
						MergeAnnotations(wrapper, other);
					}
				}
			}
		}
		
		public void Redo()
		{
			Point templateMean;
			templateMean = Utils.MeanPosition(d_network.TemplateGroup.Children);
			
			templateMean.Floor();
			
			Point parentMean;
			parentMean = Utils.MeanPosition(d_parent.Children);
			
			parentMean.Floor();

			d_import = new Wrappers.Import(d_network, d_parent, d_id, d_filename);
			
			Serialization.Project project = new Serialization.Project();

			project.Load(d_filename);
			
			// Merge annotations
			foreach (Wrappers.Wrapper wrapper in d_network.TemplateGroup.Children)
			{
				if (d_import.ImportsObject(wrapper))
				{
					MergeAnnotations(project.Network.TemplateGroup, wrapper);
					wrapper.Allocation.Move(templateMean);
					break;
				}
			}

			if (d_network.TemplateGroup != d_parent)
			{
				foreach (Wrappers.Wrapper wrapper in d_network.Children)
				{
					if (d_import == wrapper)
					{
						MergeAnnotations(project.Network, wrapper);
						wrapper.Allocation.Move(parentMean);
						break;
					}
				}
			}
		}
		
		public void Undo()
		{
			d_parent.Remove(d_import);
		}
		
		public bool CanMerge(IAction other)
		{
			return false;
		}
		
		public void Merge(IAction other)
		{
		}
		
		public bool Verify()
		{
			return true;
		}
	}
}

