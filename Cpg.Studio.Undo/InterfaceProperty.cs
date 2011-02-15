using System;

namespace Cpg.Studio.Undo
{
	public class InterfaceProperty : Object
	{
		private Wrappers.Group d_group;
		
		private string d_name;
		private string d_propid;
		
		public InterfaceProperty(Wrappers.Group grp, string name, string propid) : base(grp.Parent, grp)
		{
			d_group = grp;
			d_name = name;
			d_propid = propid;
			
			Console.WriteLine(d_propid);
		}
		
		public Wrappers.Group Group
		{
			get
			{
				return d_group;
			}
		}
		
		public void Add()
		{
			d_group.PropertyInterface.Add(d_name, d_group.FindProperty(d_propid));
		}
		
		public void Remove()
		{
			d_group.PropertyInterface.Remove(d_name);
		}
	}
}

