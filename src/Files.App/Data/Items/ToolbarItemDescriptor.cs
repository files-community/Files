// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Converters;
using static Files.App.Data.Items.ToolbarDefaultsTemplate;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Resolved toolbar item model used for display and serialization.
	/// </summary>
	public sealed partial class ToolbarItemDescriptor : ObservableObject
	{
		private const string GroupPrefix = "Group:";
		public const string SeparatorCommandCode = "Separator";

		public string CommandCode { get; }
		public string ContextId { get; }
		public string DisplayName { get; }
		public string ExtendedDisplayName { get; }
		public string ExtendedDisplayNameWithGroupSuffix => IsGroup ? $"{ExtendedDisplayName} ..." : ExtendedDisplayName;
		public string CategoryPath { get; }
		public RichGlyph Glyph { get; }
		public bool IsGroup { get; }
		public bool IsSeparator { get; }
		public bool HasIcon => !IsSeparator && !Glyph.IsNone;

		[ObservableProperty] public partial bool ShowLabel { get; set; }
		[ObservableProperty] public partial bool ShowIcon { get; set; } = true;

		public override string ToString() => DisplayName;

		private ToolbarItemDescriptor(string commandCode, string contextId, string displayName, string extendedDisplayName,
			string categoryPath, RichGlyph glyph, bool isGroup, bool isSeparator = false, bool showIcon = true, bool showLabel = false)
		{
			(CommandCode, ContextId, DisplayName, ExtendedDisplayName, CategoryPath, Glyph, IsGroup, IsSeparator) =
				(commandCode, contextId, displayName, extendedDisplayName, categoryPath, glyph, isGroup, isSeparator);
			ShowIcon = showIcon && HasIcon;
			ShowLabel = showLabel;
		}

		public static ToolbarItemDescriptor FromCommand(IRichCommand cmd, bool showIcon = true, bool showLabel = false, string? contextId = null)
			=> new(cmd.Code.ToString(), contextId ?? GetToolbarSectionId(cmd), cmd.Label, cmd.ExtendedLabel,
				GetCategoryPath(cmd), cmd.Glyph, isGroup: false, showIcon: showIcon, showLabel: showLabel);

		public static ToolbarItemDescriptor FromGroup(CommandGroup grp, bool showIcon = true, bool showLabel = false, string? contextId = null)
			=> new(GroupPrefix + grp.Name, contextId ?? GetToolbarSectionId(grp), grp.DisplayName, grp.DisplayName,
				GetCategoryPath(grp), grp.Glyph, isGroup: true, showIcon: showIcon, showLabel: showLabel);

		public static ToolbarItemDescriptor CreateSeparator(string contextId)
			=> new(SeparatorCommandCode, contextId, Strings.Separator.GetLocalizedResource(), Strings.Separator.GetLocalizedResource(),
				"", RichGlyph.None, isGroup: false, isSeparator: true, showIcon: false, showLabel: false);

		public static ToolbarItemDescriptor? Resolve(ToolbarItemSettingsEntry entry, ICommandManager commands, string contextId)
		{
			if (!string.IsNullOrEmpty(entry.CommandGroup))
				return commands.Groups.All.FirstOrDefault(g => g.Name == entry.CommandGroup) is { } group
					? FromGroup(group, entry.ShowIcon, entry.ShowLabel, contextId) : null;
			if (string.IsNullOrEmpty(entry.CommandCode)) return null;
			if (IsSeparatorCode(entry.CommandCode)) return CreateSeparator(contextId);
			return Enum.TryParse<CommandCodes>(entry.CommandCode, out var code) && code != CommandCodes.None
				? FromCommand(commands[code], entry.ShowIcon, entry.ShowLabel, contextId) : null;
		}

		public static IEnumerable<ToolbarItemDescriptor> GetAvailableItems(string contextId, ICommandManager commands)
		{
			yield return CreateSeparator(contextId);
			foreach (var group in commands.Groups.All)
				if (!string.IsNullOrEmpty(group.Name) && (group.Commands.Count == 0 || group.Commands.Any(c => c is not CommandCodes.None && commands[c].IsAccessibleGlobally)))
					yield return FromGroup(group, contextId: contextId);
			foreach (var cmd in commands)
				if (cmd.Code is not CommandCodes.None && cmd.IsAccessibleGlobally)
					yield return FromCommand(cmd, contextId: contextId);
		}

		public static List<ToolbarAvailableTreeItem> BuildAvailableTree(IEnumerable<ToolbarItemDescriptor> items)
		{
			var roots = new List<ToolbarAvailableTreeItem>();
			var nodes = new Dictionary<string, ToolbarAvailableTreeItem>(StringComparer.OrdinalIgnoreCase);
			foreach (var item in items.OrderBy(i => i.CategoryPath, StringComparer.OrdinalIgnoreCase)
				.ThenBy(i => i.ExtendedDisplayName, StringComparer.OrdinalIgnoreCase))
			{
				var parent = GetOrCreateCategoryNode(item.CategoryPath, roots, nodes);
				IList<ToolbarAvailableTreeItem> target = parent?.Children ?? (IList<ToolbarAvailableTreeItem>)roots;
				target.Add(new(item.ExtendedDisplayNameWithGroupSuffix, item));
			}
			return roots;
		}

		private static ToolbarAvailableTreeItem? GetOrCreateCategoryNode(
			string categoryPath, List<ToolbarAvailableTreeItem> roots, Dictionary<string, ToolbarAvailableTreeItem> nodes)
		{
			if (string.IsNullOrWhiteSpace(categoryPath)) return null;
			ToolbarAvailableTreeItem? parent = null;
			var path = string.Empty;
			foreach (var segment in categoryPath.Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				path = path.Length == 0 ? segment : $"{path}/{segment}";
				if (!nodes.TryGetValue(path, out var node))
				{
					node = new(segment);
					IList<ToolbarAvailableTreeItem> target = parent?.Children ?? (IList<ToolbarAvailableTreeItem>)roots;
					target.Add(node);
					nodes[path] = node;
				}
				parent = node;
			}
			return parent;
		}

		public static IEnumerable<string> BuildKnownContextIds(ICommandManager commands)
		{
			var known = new HashSet<string>(DefaultItemsByContext.Keys, StringComparer.Ordinal) { OtherContextsContextId };
			foreach (var group in commands.Groups.All)
				if (!string.IsNullOrEmpty(group.Name)) known.Add(ResolveToolbarSectionId(GroupPrefix + group.Name, commands));
			foreach (var cmd in commands)
				if (cmd.Code is not CommandCodes.None && cmd.IsAccessibleGlobally) known.Add(ResolveToolbarSectionId(cmd.Code.ToString(), commands));
			return ContextOrder.Where(known.Contains);
		}

		public static Dictionary<string, List<ToolbarItemSettingsEntry>> NormalizeSettingsByContext(
			IEnumerable<string> contextIds, IReadOnlyDictionary<string, List<ToolbarItemSettingsEntry>> saved)
		{
			var result = contextIds.ToDictionary(id => id, _ => new List<ToolbarItemSettingsEntry>(), StringComparer.Ordinal);
			foreach (var pair in saved)
				if (result.TryGetValue(NormalizeContextId(pair.Key), out var list))
					list.AddRange(pair.Value.Select(Clone));
			return result;
		}

		public static string GetContextDisplayName(string contextId) => contextId switch
		{
			AlwaysVisibleContextId => Strings.AlwaysVisible.GetLocalizedResource(),
			ArchiveFilesContextId => Strings.ArchiveFiles.GetLocalizedResource(),
			BatchFilesContextId => Strings.BatchFiles.GetLocalizedResource(),
			PowerShellFilesContextId => Strings.PowerShellFiles.GetLocalizedResource(),
			ImageFilesContextId => Strings.ImageFiles.GetLocalizedResource(),
			MediaFilesContextId => Strings.MediaFiles.GetLocalizedResource(),
			FontFilesContextId => Strings.FontFiles.GetLocalizedResource(),
			DriverFilesContextId => Strings.DriverFiles.GetLocalizedResource(),
			CertificateFilesContextId => Strings.CertificateFiles.GetLocalizedResource(),
			RecycleBinContextId => Strings.RecycleBin.GetLocalizedResource(),
			_ => Strings.OtherContexts.GetLocalizedResource(),
		};

		public static string ResolveToolbarSectionId(string commandCode, ICommandManager commands)
		{
			if (commandCode.StartsWith(GroupPrefix, StringComparison.Ordinal))
				return commands.Groups.All.FirstOrDefault(c => c.Name == commandCode[GroupPrefix.Length..]) is { } group
					? GetToolbarSectionId(group) : OtherContextsContextId;
			return Enum.TryParse<CommandCodes>(commandCode, out var code) && code != CommandCodes.None
				? GetToolbarSectionId(commands[code]) : OtherContextsContextId;
		}

		public ToolbarItemSettingsEntry ToSettingsEntry() => IsGroup
			? new(commandGroup: CommandCode[GroupPrefix.Length..], showIcon: HasIcon && ShowIcon, showLabel: ShowLabel)
			: new(commandCode: IsSeparator ? SeparatorCommandCode : CommandCode, showIcon: HasIcon && ShowIcon, showLabel: ShowLabel);

		public static bool IsGroupCode(string code) => code.StartsWith(GroupPrefix, StringComparison.Ordinal);
		public static bool IsSeparatorCode(string code) => string.Equals(code, SeparatorCommandCode, StringComparison.Ordinal);
		public static string GetGroupName(string code) => code[GroupPrefix.Length..];

		private static string GetToolbarSectionId(CommandGroup group) => group.Name switch
		{
			nameof(CommandGroups.Extract) => ArchiveFilesContextId,
			nameof(CommandGroups.SetAs) => ImageFilesContextId,
			_ => AlwaysVisibleContextId,
		};

		private static string GetToolbarSectionId(IRichCommand cmd) => cmd.Code switch
		{
			CommandCodes.DecompressArchiveToChildFolder => ArchiveFilesContextId,
			CommandCodes.RunWithPowershell => PowerShellFilesContextId,
			CommandCodes.EditInNotepad => BatchFilesContextId,
			CommandCodes.SetAsWallpaperBackground or CommandCodes.SetAsLockscreenBackground or CommandCodes.SetAsAppBackground
				or CommandCodes.SetAsSlideshowBackground or CommandCodes.RotateLeft or CommandCodes.RotateRight => ImageFilesContextId,
			CommandCodes.PlayAll => MediaFilesContextId,
			CommandCodes.InstallFont => FontFilesContextId,
			CommandCodes.InstallInfDriver => DriverFilesContextId,
			CommandCodes.InstallCertificate => CertificateFilesContextId,
			CommandCodes.EmptyRecycleBin or CommandCodes.RestoreAllRecycleBin or CommandCodes.RestoreRecycleBin => RecycleBinContextId,
			_ => OtherContextsContextId,
		};

		private static string GetCategoryPath(IRichCommand cmd)
			=> cmd is ActionCommand { Action: { Category: not ActionCategory.Unspecified } action }
				? ActionCategoryConverter.ToLocalizedCategoryPath(action.Category) : Strings.General.GetLocalizedResource();

		private static string GetCategoryPath(CommandGroup grp) => ActionCategoryConverter.ToLocalizedCategoryPath(grp.Category);
	}

	public sealed class ToolbarAvailableTreeItem(string displayName, ToolbarItemDescriptor? toolbarItem = null)
	{
		public string DisplayName { get; } = displayName;
		public ToolbarItemDescriptor? ToolbarItem { get; } = toolbarItem;
		public ObservableCollection<ToolbarAvailableTreeItem> Children { get; } = [];
		public override string ToString() => DisplayName;
	}
}
