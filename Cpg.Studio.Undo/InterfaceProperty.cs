using System;

namespace Cpg.Studio.Undo
{
	public class InterfaceProperty : Object
	{
		private Wrappers.Group d_group;
		
		private string d_name;
		private string d_childname;
		private string d_propid;
		
		public InterfaceProperty(Wrappers.Group grp, string name, string childname, string propid) : base(grp.Parent, grp)
		{
			d_group = grp;
			d_name = name;
			d_childname = childname;
			d_propid = propid;
		}
		
		public Wrappers.Group Group
		{
			get
			{
				return d_group;
			}
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public string ChildName
		{
			get
			{
				return d_childname;
			}
		}
		
		public string PropertyId
		{
			get
			{
				return d_propid;
			}
		}
		
		public void Add()
		{
			d_group.PropertyInterface.Add(d_name, d_childname, d_propid);
		}
		
		public void Remove()
		{
			d_group.PropertyInterface.Remove(d_name);
		}
	}
}

