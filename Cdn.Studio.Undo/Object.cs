using System;
using System.Collections.Generic;

namespace Cdn.Studio.Undo
{
	public class Object
	{
		private Wrappers.Group d_parent;
		private Wrappers.Wrapper d_wrapped;
		
		private Wrappers.Wrapper d_from;
		private Wrappers.Wrapper d_to;
		
		private List<Wrappers.Wrapper> d_templates;

		public Object(Wrappers.Group parent, Wrappers.Wrapper wrapped)
		{
			d_parent = parent;
			d_wrapped = wrapped;
			
			d_templates = new List<Wrappers.Wrapper>();
			
			Wrappers.Link link = wrapped as Wrappers.Link;
			
			if (link != null)
			{
				d_from = link.From;
				d_to = link.To;
			}
		}
		
		protected void DoAdd()
		{
			Wrappers.Link link = d_wrapped as Wrappers.Link;
			
			if (link != null)
			{
				link.Attach(d_from, d_to);
			}
			
			foreach (Wrappers.Wrapper templ in d_templates)
			{
				d_wrapped.ApplyTemplate(templ);
			}

			d_parent.Add(d_wrapped);
		}
		
		protected void DoRemove()
		{
			Wrappers.Wrapper[] appliedto = d_wrapped.TemplateAppliesTo;
			
			if (appliedto.Length != 0)
			{
				if (appliedto.Length > 5)
				{
					throw new Exception(String.Format("The template is still in use by {0} objects", appliedto.Length));
				}
				else
				{
					string names = String.Join(", ", Array.ConvertAll<Wrappers.Wrapper, string>(appliedto, a => a.FullId));
					throw new Exception(String.Format("The template is still in use by `{0}'", names));
				}
			}

			Wrappers.Link link = d_wrapped as Wrappers.Link;
			
			if (link != null && link.To != null)
			{
				// Do this so that link offsets are recalculated correctly. This is a bit of a hack
				// really, and might be solved differently in the distant future, the year 2000
				link.To.Unlink(link);
			}
			
			d_templates = new List<Wrappers.Wrapper>(d_wrapped.AppliedTemplates);
			
			d_parent.Remove(d_wrapped);
			d_wrapped.Removed();
			
			for (int i = d_templates.Count - 1; i >= 0; --i)
			{
				d_wrapped.UnapplyTemplate(d_templates[i]);
			}
		}
		
		public Wrappers.Wrapper Wrapped
		{
			get
			{
				return d_wrapped;
			}
		}
		
		public virtual bool CanMerge(IAction other)
		{
			return false;
		}
		
		public virtual void Merge(IAction other)
		{
		}
		
		public virtual bool Verify()
		{
			return true;
		}
	}
}
