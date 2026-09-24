// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Files.App.Helpers
{
	public static class FolderAliasHelpers
	{
		public const char Prefix = '/';

		private static readonly char[] Separators = ['\\', '/'];

		public static IReadOnlyList<FolderAliasItem> Aliases
			=> Ioc.Default.GetRequiredService<IFoldersSettingsService>().FolderAliases ?? [];

		/// <summary>
		/// Gets whether the input is a bare alias query such as "/pro", with no sub path.
		/// </summary>
		public static bool IsAliasQuery(string input)
			=> input.Length > 1 && input[0] == Prefix && input.IndexOfAny(Separators, 1) < 0;

		public static bool IsValidName(string name)
			=> !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Separators) < 0;

		public static IEnumerable<FolderAliasItem> GetMatches(IEnumerable<FolderAliasItem> aliases, string input)
		{
			var query = input.TrimStart(Prefix);

			return aliases
				.Where(x => x.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Resolves "/alias" or "/alias\sub\path" to the aliased folder, keeping any sub path.
		/// </summary>
		public static bool TryResolve(string input, [NotNullWhen(true)] out string? resolvedPath)
		{
			resolvedPath = null;

			if (input.Length < 2 || input[0] != Prefix || Array.IndexOf(Separators, input[1]) >= 0)
				return false;

			var separatorIndex = input.IndexOfAny(Separators, 1);
			var name = separatorIndex < 0 ? input[1..] : input[1..separatorIndex];
			var alias = Aliases.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (alias is null)
				return false;

			var subPath = separatorIndex < 0 ? string.Empty : input[(separatorIndex + 1)..];
			resolvedPath = string.IsNullOrEmpty(subPath) ? alias.Path : Path.Combine(alias.Path, subPath);
			return true;
		}
	}
}
