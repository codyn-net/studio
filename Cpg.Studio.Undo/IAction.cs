using System;

namespace Cpg.Studio.Undo
{
	public interface IAction
	{
		void Undo();
		void Redo();
		
		bool CanMerge(IAction other);
		void Merge(IAction other);
		
		bool Verify();
		
		string Description
		{
			get;
		}
	}
}
