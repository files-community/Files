// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a rule that chooses the layout template of the folders it matches.
	/// </summary>
	public sealed partial class LayoutRule
	{
		public LayoutRuleKind Kind { get; set; }

		/// <summary>
		/// Gets or sets the folder matched by a <see cref="LayoutRuleKind.Folder"/> rule, either a path or a <see cref="LayoutKnownFolders"/> name.
		/// </summary>
		public string Path { get; set; } = string.Empty;

		public bool IncludeSubfolders { get; set; }

		/// <summary>
		/// Gets or sets the file extensions matched by a <see cref="LayoutRuleKind.FileTypes"/> rule, separated by commas.
		/// </summary>
		public string Extensions { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the share of items, in percent, that <see cref="LayoutRuleKind.FileTypes"/> rules require.
		/// </summary>
		public int Threshold { get; set; } = 60;

		public string TemplateId { get; set; } = string.Empty;

		[JsonIgnore]
		public bool IsContentRule
			=> Kind is LayoutRuleKind.FileTypes;

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
