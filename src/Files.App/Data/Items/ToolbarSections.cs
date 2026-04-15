// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Factory-default toolbar layout, context ordering, and settings merge helpers.
	/// </summary>
	public static class ToolbarDefaultsTemplate
	{
		public const string AlwaysVisibleContextId = "AlwaysVisible";
		public const string ArchiveFilesContextId = "ArchiveFiles";
		public const string ScriptFilesContextId = "ScriptFiles";
		public const string ImageFilesContextId = "ImageFiles";
		public const string MediaFilesContextId = "MediaFiles";
		public const string FontFilesContextId = "FontFiles";
		public const string DriverFilesContextId = "DriverFiles";
		public const string CertificateFilesContextId = "CertificateFiles";
		public const string RecycleBinContextId = "RecycleBin";
		public const string OtherContextsContextId = "OtherContexts";

		public static readonly IReadOnlyDictionary<string, ToolbarItemSettingsEntry[]> DefaultItemsByContext =
			new Dictionary<string, ToolbarItemSettingsEntry[]>(StringComparer.Ordinal)
			{
				[AlwaysVisibleContextId] =
				[
					new(commandGroup: nameof(CommandGroups.NewItem), showLabel: true),
					new(commandCode: ToolbarItemDescriptor.SeparatorCommandCode, showIcon: false),
					new(commandCode: nameof(CommandCodes.CutItem)),
					new(commandCode: nameof(CommandCodes.CopyItem)),
					new(commandCode: nameof(CommandCodes.PasteItem)),
					new(commandCode: nameof(CommandCodes.Rename)),
					new(commandCode: nameof(CommandCodes.ShareItem)),
					new(commandCode: nameof(CommandCodes.DeleteItem)),
					new(commandCode: nameof(CommandCodes.OpenProperties)),
				],
				[ArchiveFilesContextId] = [new(commandGroup: nameof(CommandGroups.Extract), showLabel: true)],
				[ScriptFilesContextId] =
				[
					new(commandCode: nameof(CommandCodes.RunWithPowershell), showLabel: true),
					new(commandCode: nameof(CommandCodes.EditInNotepad), showLabel: true),
				],
				[ImageFilesContextId] =
				[
					new(commandGroup: nameof(CommandGroups.SetAs), showLabel: true),
					new(commandCode: nameof(CommandCodes.SetAsSlideshowBackground), showLabel: true),
					new(commandCode: nameof(CommandCodes.RotateLeft), showLabel: true),
					new(commandCode: nameof(CommandCodes.RotateRight), showLabel: true),
				],
				[MediaFilesContextId] = [new(commandCode: nameof(CommandCodes.PlayAll), showLabel: true)],
				[FontFilesContextId] = [new(commandCode: nameof(CommandCodes.InstallFont), showLabel: true)],
				[DriverFilesContextId] = [new(commandCode: nameof(CommandCodes.InstallInfDriver), showLabel: true)],
				[CertificateFilesContextId] = [new(commandCode: nameof(CommandCodes.InstallCertificate), showLabel: true)],
				[RecycleBinContextId] =
				[
					new(commandCode: nameof(CommandCodes.EmptyRecycleBin), showLabel: true),
					new(commandCode: nameof(CommandCodes.RestoreAllRecycleBin), showLabel: true),
					new(commandCode: nameof(CommandCodes.RestoreRecycleBin), showLabel: true),
				],
			};

		public static readonly string[] ContextOrder =
		[
			AlwaysVisibleContextId,
			.. DefaultItemsByContext.Keys.Where(static k => k != AlwaysVisibleContextId),
			OtherContextsContextId,
		];

		private static readonly HashSet<string> KnownContextSet = new(ContextOrder, StringComparer.Ordinal);

		public static Dictionary<string, List<ToolbarItemSettingsEntry>> CreateDefaultItemsByContext()
			=> ContextOrder.ToDictionary(id => id, id => new List<ToolbarItemSettingsEntry>(
				Array.ConvertAll(DefaultItemsByContext.GetValueOrDefault(id) ?? [], Clone)), StringComparer.Ordinal);

		public static Dictionary<string, List<ToolbarItemSettingsEntry>> ResolveToolbarItemsByContext(IAppearanceSettingsService settings)
		{
			var saved = settings.CustomToolbarItems;
			var hasExisting = saved is { Count: > 0 };
			var items = saved is { Count: > 0 } existing
				? existing.ToDictionary(p => p.Key, p => p.Value.ConvertAll(Clone), StringComparer.Ordinal)
				: CreateDefaultItemsByContext();

			var currentIds = ContextOrder.ToDictionary(id => id, id => (DefaultItemsByContext.GetValueOrDefault(id) ?? [])
				.Select(e => e.CommandCode ?? e.CommandGroup).OfType<string>().ToList(), StringComparer.Ordinal);
			var previous = settings.LastKnownToolbarDefaults;

			if (hasExisting
				&& previous is { Count: > 0 }
				&& MergeNewDefaults(items, previous))
				settings.CustomToolbarItems = items;

			if (previous is null || previous.Count != currentIds.Count
				|| !currentIds.All(kv => previous.TryGetValue(kv.Key, out var ids) && ids.SequenceEqual(kv.Value, StringComparer.Ordinal)))
				settings.LastKnownToolbarDefaults = currentIds;

			return items;
		}

		private static bool MergeNewDefaults(
			Dictionary<string, List<ToolbarItemSettingsEntry>> items,
			IReadOnlyDictionary<string, List<string>> previousTemplate)
		{
			var changed = false;
			foreach (var contextId in ContextOrder)
			{
				var prevIds = new HashSet<string>(previousTemplate.GetValueOrDefault(contextId) ?? [], StringComparer.Ordinal);
				if (!items.TryGetValue(contextId, out var list))
					items[contextId] = list = [];

				var existing = list.Select(e => e.CommandCode ?? e.CommandGroup ?? "").ToHashSet(StringComparer.Ordinal);
				foreach (var def in DefaultItemsByContext.GetValueOrDefault(contextId) ?? [])
				{
					var id = def.CommandCode ?? def.CommandGroup;
					if (!string.IsNullOrEmpty(id)
						&& !ToolbarItemDescriptor.IsSeparatorCode(id)
						&& !prevIds.Contains(id)
						&& existing.Add(id))
					{
						list.Add(Clone(def));
						changed = true;
					}
				}
			}
			return changed;
		}

		public static string NormalizeContextId(string? contextId,
			string nullFallback = OtherContextsContextId, string unknownFallback = OtherContextsContextId)
		{
			if (string.IsNullOrEmpty(contextId))
				return nullFallback;

			return KnownContextSet.Contains(contextId) ? contextId : unknownFallback;
		}

		internal static ToolbarItemSettingsEntry Clone(ToolbarItemSettingsEntry e)
			=> new(commandCode: e.CommandCode, commandGroup: e.CommandGroup, showIcon: e.ShowIcon, showLabel: e.ShowLabel);
	}
}
