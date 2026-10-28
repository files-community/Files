// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a rule that chooses a layout template for folders that mostly contain certain file types.
	/// </summary>
	public sealed partial class LayoutRule
	{
		/// <summary>
		/// Gets or sets the file extensions the rule matches, separated by commas.
		/// </summary>
		public string Extensions { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the share of items, in percent, that the rule requires.
		/// </summary>
		public int Threshold { get; set; } = 60;

		public string TemplateId { get; set; } = string.Empty;

		/// <summary>
		/// Gets the extensions of <see cref="Extensions"/>, each starting with a dot.
		/// </summary>
		public IEnumerable<string> GetExtensions()
		{
			return (Extensions ?? string.Empty)
				.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
				.Distinct(StringComparer.OrdinalIgnoreCase);
		}

		public LayoutRule Clone()
			=> (LayoutRule)MemberwiseClone();
	}
}
