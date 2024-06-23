﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item of ini file's section.
	/// </summary>
	public class IniSectionDataItem
	{
		public string SectionName { get; set; } = "";

		public Dictionary<string, string>? Parameters { get; set; }
	}
}
