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
				.Select(NormalizeExtension)
				.Distinct(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Gets whether the specified extension, as returned by <see cref="GetExtensions"/>, can match a file name.
		/// </summary>
		public static bool IsValidExtension(string extension)
			=> extension.Length > 1 && extension.IndexOfAny(SystemIO.Path.GetInvalidFileNameChars()) < 0;

		private static string NormalizeExtension(string extension)
		{
			var trimmed = extension.TrimStart('*');
			return trimmed.StartsWith('.') ? trimmed : $".{trimmed}";
		}

		public LayoutRule Clone()
			=> (LayoutRule)MemberwiseClone();
	}
}
