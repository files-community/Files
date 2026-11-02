// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Server;

internal sealed partial class ModernContextMenuBackend : IDisposable
{
	// Above the classic menu's command IDs
	private const int FirstCommandId = 0x8000;
	private const int MaximumDepth = 8;
	private const int MaximumCommands = 1024;

	private readonly IShellItemArray selection;
	private readonly List<IExplorerCommand> commands = [];
	private readonly List<IExplorerCommand> ownedCommands = [];
	private readonly HashSet<string> commandStrings = new(StringComparer.OrdinalIgnoreCase);

	// Icons and subcommands are slow, so they load after the items
	private readonly List<(ContextMenuItem Item, IExplorerCommand? Command, string? IconLocation, int Depth)> details = [];

	public List<Win32ContextMenuItem> Items { get; } = [];

	// For the background, the selection is the folder, as there's no Explorer view site to get it from
	internal ModernContextMenuBackend(IShellItemArray selection, ShellItem[] items, bool isBackground)
	{
		this.selection = selection;
		try
		{
			AddCommands(GetRegistrations(items, isBackground));
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not load packaged context menu commands.");
		}
	}

	// Safe on any thread, as it's listed on creation
	public bool HasCommand(string? commandString)
		=> !string.IsNullOrEmpty(commandString) && commandStrings.Contains(commandString);

	public static bool HasItem(int itemId)
		=> itemId >= FirstCommandId;

	public HRESULT InvokeItem(int itemId)
	{
		var index = itemId - FirstCommandId;
		if ((uint)index >= (uint)commands.Count)
			return HRESULT.E_INVALIDARG;

		// The command state can change while the menu is open
		var command = commands[index];
		var result = command.GetState(selection, true, out var state);
		if (result.Failed)
			return result;

		return (state & (_EXPCMDSTATE.ECS_HIDDEN | _EXPCMDSTATE.ECS_DISABLED)) == 0 ? command.Invoke(selection, null) : HRESULT.E_FAIL;
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

	private void AddCommands(IEnumerable<ExplorerCommandRegistration> registrations)
	{
		var groups = registrations
			.GroupBy(registration => (registration.PackageFullName, registration.DisplayName))
			.OrderByDescending(group => group.Any(registration => registration.IsCloudProvider))
			.ThenBy(group => group.Key.DisplayName, StringComparer.CurrentCultureIgnoreCase);

		foreach (var group in groups)
		{
			var groupItems = new List<Win32ContextMenuItem>();
			foreach (var registration in group)
			{
				try
				{
					if (CreateCommand(registration) is { } command)
						AddCommand(command, registration.ClassId.ToString("B").ToUpperInvariant(), groupItems, 0);
				}
				catch (Exception ex)
				{
					Program.Logger.LogDebug(ex, "Could not load packaged context menu command {ClassId}.", registration.ClassId);
				}
			}

			var commandCount = groupItems.Count(item => item.Type != MENU_ITEM_TYPE.MFT_SEPARATOR);
			if (commandCount == 0)
				continue;

			if (commandCount == 1 || string.IsNullOrEmpty(group.Key.DisplayName))
			{
				Items.AddRange(groupItems);
			}
			else
			{
				var groupItem = new ContextMenuItem
				{
					ID = -1,
					Label = group.Key.DisplayName,
					SubItems = groupItems,
				};
				Items.Add(groupItem);
				details.Add((groupItem, null, group.Select(registration => registration.GroupIcon).FirstOrDefault(icon => !string.IsNullOrEmpty(icon)), 0));
			}
		}
	}

	public void LoadDetails()
	{
		// Grows with the subcommands found
		for (var index = 0; index < details.Count; index++)
		{
			var (item, command, iconLocation, depth) = details[index];
			item.Icon = command is null ? LoadIcon(iconLocation) : GetIcon(command);
			if (command is null || item.SubItems is not { } subItems)
				continue;

			AddSubCommands(command, subItems, depth + 1);

			// Without visible subcommands, the command is invoked itself
			if (!subItems.Any(subItem => subItem.Type != MENU_ITEM_TYPE.MFT_SEPARATOR))
				item.SubItems = null;
		}

		details.Clear();
	}

	private void AddCommand(IExplorerCommand command, string fallbackCommandString, List<Win32ContextMenuItem> target, int depth)
	{
		ownedCommands.Add(command);
		if (commands.Count >= MaximumCommands || depth > MaximumDepth)
			return;

		if (command.GetState(selection, true, out var state).Failed || state.HasFlag(_EXPCMDSTATE.ECS_HIDDEN))
			return;

		if (command.GetFlags(out var flags).Failed)
			flags = _EXPCMDFLAGS.ECF_DEFAULT;

		if (flags.HasFlag(_EXPCMDFLAGS.ECF_ISSEPARATOR))
		{
			target.Add(CreateSeparator());
			return;
		}

		if (command.GetTitle(selection, out var title).Failed || string.IsNullOrEmpty(title))
			return;

		// The canonical name matches the verb of the same command in the classic menu
		var commandString = command.GetCanonicalName(out var canonicalName).Succeeded && canonicalName != Guid.Empty
			? canonicalName.ToString("B").ToUpperInvariant()
			: fallbackCommandString;
		var item = new ContextMenuItem
		{
			ID = FirstCommandId + commands.Count,
			Label = title,
			CommandString = commandString,
			IsEnabled = !state.HasFlag(_EXPCMDSTATE.ECS_DISABLED),
			IsChecked = state.HasFlag(_EXPCMDSTATE.ECS_CHECKED),
			IsToggle = (state & (_EXPCMDSTATE.ECS_CHECKBOX | _EXPCMDSTATE.ECS_RADIOCHECK)) != 0 || flags.HasFlag(_EXPCMDFLAGS.ECF_TOGGLEABLE),
			SubItems = flags.HasFlag(_EXPCMDFLAGS.ECF_HASSUBCOMMANDS) ? [] : null,
		};
		commands.Add(command);
		details.Add((item, command, null, depth));

		// The classic menu also lists the top-level commands
		if (depth == 0 && !string.IsNullOrEmpty(commandString))
			commandStrings.Add(commandString);

		if (flags.HasFlag(_EXPCMDFLAGS.ECF_SEPARATORBEFORE))
			target.Add(CreateSeparator());
		target.Add(item);
		if (flags.HasFlag(_EXPCMDFLAGS.ECF_SEPARATORAFTER))
			target.Add(CreateSeparator());
	}

	private void AddSubCommands(IExplorerCommand parent, List<Win32ContextMenuItem> target, int depth)
	{
		if (depth > MaximumDepth || parent.EnumSubCommands(out var enumerator).Failed || enumerator is null)
			return;

		try
		{
			var buffer = new IExplorerCommand?[1];
			for (var index = 0; index < MaximumCommands && commands.Count < MaximumCommands; index++)
			{
				if (enumerator.Next(1, buffer, out var fetched).Failed || fetched != 1 || buffer[0] is not { } command)
					break;

				buffer[0] = null;
				try
				{
					AddCommand(command, string.Empty, target, depth);
				}
				catch (Exception ex)
				{
					Program.Logger.LogDebug(ex, "Could not load a packaged context menu subcommand.");
				}
			}
		}
		finally
		{
			Release(enumerator);
		}
	}

	private static IExplorerCommand? CreateCommand(ExplorerCommandRegistration registration)
	{
		if (ExplorerCommandActivator.Create(registration.ClassId) is not { } command)
			return null;

		if (command is IInitializeCommand initializer && registration.Verb.Length > 0 && initializer.Initialize(registration.Verb, null!).Failed)
		{
			Release(command);
			return null;
		}

		return command;
	}

	private byte[]? GetIcon(IExplorerCommand command)
	{
		try
		{
			return command.GetIcon(selection, out var location).Succeeded ? LoadIcon(location) : null;
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not load a packaged context menu icon.");
			return null;
		}
	}

	private static byte[]? LoadIcon(string? location)
	{
		if (string.IsNullOrEmpty(location))
			return null;

		try
		{
			var index = 0;
			var separator = location.LastIndexOf(',');
			if (separator >= 0 && int.TryParse(location.AsSpan(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
				location = location[..separator];
			location = Environment.ExpandEnvironmentVariables(location.Trim().Trim('"'));

			// ShellImage negates the index for SHDefExtractIcon
			return ShellImage.ExtractSelectedIconsFromDLL(location, [-index], 24).FirstOrDefault()?.IconData;
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not load a packaged context menu icon.");
			return null;
		}
	}

	private static ContextMenuItem CreateSeparator()
		=> new() { ID = -1, Type = MENU_ITEM_TYPE.MFT_SEPARATOR };

	private static void Release(object? value)
	{
		if (value is ComObject comObject)
			comObject.FinalRelease();
	}

	public void Dispose()
	{
		foreach (var item in Items)
			(item as IDisposable)?.Dispose();
		foreach (var command in ownedCommands)
			Release(command);
		ownedCommands.Clear();
		commands.Clear();
	}
}
