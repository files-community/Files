// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal sealed class PackagedCommandRegistrations(List<IGrouping<string, ExplorerCommandRegistration>> groups, HashSet<string> classicVerbs)
{
	// Grouped by app, cloud providers first
	public List<IGrouping<string, ExplorerCommandRegistration>> Groups { get; } = groups;

	// Explorer adds the commands to the classic menu too, with their class IDs as verbs
	public HashSet<string> ClassicVerbs { get; } = classicVerbs;

	public static PackagedCommandRegistrations Get(ShellItem[] items, bool isBackground)
	{
		var registrations = GetRegistrations(items, isBackground);

		// For the background, the classic menu is the folder's own
		var classicRegistrations = isBackground ? GetRegistrations(items, false) : registrations;
		return new(registrations
				.GroupBy(registration => registration.GroupId)
				.OrderByDescending(group => group.Any(registration => registration.IsCloudProvider))
				.ThenBy(group => group.First().DisplayName, StringComparer.CurrentCultureIgnoreCase)
				.ToList(),
			new(classicRegistrations.Select(registration => registration.ClassId.ToString("B")), StringComparer.OrdinalIgnoreCase));
	}

	private static IEnumerable<ExplorerCommandRegistration> GetRegistrations(ShellItem[] items, bool isBackground)
	{
		if (items.Any(item => !item.IsFileSystem) || (isBackground && (items.Length != 1 || !items[0].IsFolder || items[0].IsStream)))
			return [];

		// A command applies when it is registered for every selected item type
		HashSet<ExplorerCommandRegistration>? registrations = null;
		string[] itemTypes = isBackground ? [@"Directory\Background"] : [.. items.Select(GetItemType).Distinct(StringComparer.OrdinalIgnoreCase)];
		foreach (var itemType in itemTypes)
		{
			var matches = FileExplorerAppExtensions.GetCommands(itemType switch
			{
				"Directory" or "Drive" or @"Directory\Background" => [itemType],
				"" => ["*"],
				_ => ["*", itemType],
			});

			if (registrations is null)
				registrations = matches;
			else
				registrations.IntersectWith(matches);
		}

		registrations ??= [];

		// Cloud provider commands are grouped under the provider
		foreach (var cloudCommand in CloudContextMenuCommands.GetCommands(items.Select(item => item.FileSystemPath!)))
		{
			registrations.RemoveWhere(registration => registration.ClassId == cloudCommand.ClassId);
			registrations.Add(cloudCommand);
		}

		return registrations;
	}

	private static string GetItemType(ShellItem item)
	{
		var path = item.FileSystemPath ?? string.Empty;
		if (!item.IsFolder || item.IsStream)
			return Path.GetExtension(path);

		return Path.GetPathRoot(path) is { Length: 3 } root && root[1] == ':' && path.Equals(root, StringComparison.OrdinalIgnoreCase)
			? "Drive"
			: "Directory";
	}
}
