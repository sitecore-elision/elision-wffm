using System;

namespace Elision.Feature.Library.Wffm.ActionFilters
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class WfmFormAttribute : Attribute
	{
		public string FormName { get; private set; }

		public WfmFormAttribute(string formName)
		{
			FormName = formName;
		}
	}
}