using System;

namespace Cdn.Studio.Undo
{
	public class InterfaceVariable : Object
	{
		private Wrappers.Node d_group;
		private string d_name;
		private string d_childname;
		private string d_variableid;
		
		public InterfaceVariable(Wrappers.Node grp, string name, string childname, string propid) : base(grp.Parent, grp)
		{
			d_group = grp;
			d_name = name;
			d_childname = childname;
			d_variableid = propid;
		}
		
		public Wrappers.Node Group
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
		
		public string VariableId
		{
			get
			{
				return d_variableid;
			}
		}
		
		public void Add()
		{
			d_group.VariableInterface.Add(d_name, d_childname, d_variableid);
		}
		
		public void Remove()
		{
			d_group.VariableInterface.Remove(d_name);
		}
	}
}

