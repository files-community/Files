// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Immutable;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item of ini file's section.
	/// </summary>
	public class IniSectionDataItem
	{
		/// <summary>
		/// Gets or sets the name of this item's section.
		/// </summary>
		public string SectionName { get; set; } = "";

		/// <summary>
		/// Gets or sets all parameters available in the section.
		/// </summary>
		public IDictionary<string, string> Parameters { get; set; } = ImmutableDictionary<string, string>.Empty;
	}
}
