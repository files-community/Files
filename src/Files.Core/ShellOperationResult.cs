using System.Collections.Generic;
using System.Linq;

namespace Files.Core
{
	public class ShellOperationResult
	{
		public ShellOperationResult()
		{
			Items = new List<ShellOperationItemResult>();
		}

		/// <summary>
		/// File operation results: success and error code. Can contains multiple results for the same source file.
		/// E.g. if the shell shows a "replace" confirmation dialog, results can be both COPYENGINE_S_PENDING and COPYENGINE_S_USER_IGNORED.
		/// </summary>
		public List<ShellOperationItemResult> Items { get; set; }

		/// <summary>
		/// Final results of a file operation. Contains last status for each source file.
		/// </summary>
		public List<ShellOperationItemResult> Final =>
			Items.GroupBy(x => new { Src = x.Source, Dst = x.Destination }).Select(x => x.Last()).ToList();
	}

	public class ShellOperationItemResult
	{
		public bool Succeeded { get; set; }
		public int HResult { get; set; }
		public string Source { get; set; }
		public string Destination { get; set; }
	}
}
