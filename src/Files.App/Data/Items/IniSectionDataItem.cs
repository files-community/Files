// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Immutable;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item of ini file's section.
	/// </summary>
	public class IniSectionDataItem
	{
		public string SectionName { get; set; } = "";

		public IDictionary<string, string> Parameters { get; set; } = ImmutableDictionary<string, string>.Empty;
	}
}
