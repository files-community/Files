using System;
using System.Collections.Generic;

#nullable enable

namespace Files.Backend.Models.Coloring
{
	[Serializable]
	public abstract class ColorModel : ICustomFormattable
	{
		public virtual IReadOnlyCollection<string>? Formats { get; }

		public virtual void AppendFormat(string formatInfo) { }
	}
}
