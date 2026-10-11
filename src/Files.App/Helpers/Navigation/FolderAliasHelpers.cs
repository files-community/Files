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

		private static IReadOnlyList<FolderAliasItem>? _aliases;

		private static bool _isSubscribed;

		/// <summary>
		/// Gets the configured aliases, cached until the setting changes.
		/// </summary>
		public static IReadOnlyList<FolderAliasItem> Aliases
		{
			get
			{
				if (_aliases is { } aliases)
					return aliases;

				var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
				if (!_isSubscribed)
				{
					userSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
					_isSubscribed = true;
				}

				return _aliases = userSettingsService.FoldersSettingsService.FolderAliases ?? [];
			}
		}

		/// <summary>
		/// Gets whether the input is a bare alias query such as "/pro", with no sub path.
		/// </summary>
		public static bool IsAliasQuery(string input)
			=> input.Length > 1 && input[0] == Prefix && input.IndexOfAny(Separators, 1) < 0;

		/// <summary>
		/// Gets the alias name typed after the prefix, ignoring surrounding spaces.
		/// </summary>
		public static string GetQuery(string input)
			=> input[1..].Trim();

		public static bool IsValidName(string name)
			=> !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Separators) < 0;

		public static IEnumerable<FolderAliasItem> GetMatches(IEnumerable<FolderAliasItem> aliases, string input)
		{
			var query = GetQuery(input);

			return aliases
				.Where(x => x.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Resolves "/alias" or "/alias\sub\path" to the aliased folder, keeping any sub path.
		/// </summary>
		public static bool TryResolve(string input, IReadOnlyList<FolderAliasItem> aliases, [NotNullWhen(true)] out string? resolvedPath)
		{
			resolvedPath = null;

			if (input.Length < 2 || input[0] != Prefix || Array.IndexOf(Separators, input[1]) >= 0)
				return false;

			var separatorIndex = input.IndexOfAny(Separators, 1);
			var name = (separatorIndex < 0 ? input[1..] : input[1..separatorIndex]).Trim();
			var alias = aliases.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (alias is null)
				return false;

			var subPath = separatorIndex < 0 ? string.Empty : input[(separatorIndex + 1)..].TrimStart(Separators);
			resolvedPath = string.IsNullOrEmpty(subPath) ? alias.Path : Path.Combine(alias.Path, subPath);
			return true;
		}

		private static void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			if (e.SettingName is nameof(IFoldersSettingsService.FolderAliases))
				_aliases = null;
		}
	}
}
