using System.Collections.Generic;

namespace Elision.Feature.Library.Wffm.Models
{
	public class WfmFormSubmitResults
	{
		public WfmFormSubmitStatus Status { get; set; }
		public IEnumerable<string> Messages { get; set; }
		public WfmSuccessAction SuccessAction { get; set; }
		public string SuccessMessage { get; set; }
		public string SuccessRedirect { get; set; }
	}

	public enum WfmFormSubmitStatus
	{
		Unknown,
		Success,
		Failure
	}

	public enum WfmSuccessAction
	{
		Redirect,
		Message
	}
}