using System.Collections.Generic;

namespace Files.Core.Models
{
	public interface ICustomFormattable
	{
		IReadOnlyCollection<string>? Formats { get; }

		bool AppendFormat(string formatInfo);
	}
}
