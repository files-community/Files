#nullable enable

using System.Collections.Generic;

namespace Files.Backend.Models.Imaging
{
	public abstract class ImageModel : ICustomFormattable
	{
		public virtual IReadOnlyCollection<string>? Formats { get; }

		public virtual void AppendFormat(string formatInfo) { }
	}
}
