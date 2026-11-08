// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;
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
	private readonly bool isBackground;
	private readonly FolderViewSite? site;
	private readonly int firstCommandId;
	private readonly List<(IExplorerCommand Command, IShellItemArray? Selection)> commands = [];
	private readonly List<IExplorerCommand> ownedCommands = [];
	private readonly HashSet<string> titles = new(StringComparer.Ordinal);

	// Icons and subcommands are slow, so they load after the items
	private readonly List<(ContextMenuItem Item, IExplorerCommand? Command, IShellItemArray? Selection, string? IconLocation, int Depth)> details = [];

	// The submenus of the items, complete once the details have loaded
	private readonly HashSet<List<Win32ContextMenuItem>> submenus = [];

	public List<Win32ContextMenuItem> Items { get; } = [];

	// The commands of one app, with an ID range of their own. For the background, the selection is the folder itself
	internal ModernContextMenuBackend(IShellItemArray selection, ShellItem[] items, bool isBackground, IGrouping<string, ExplorerCommandRegistration> group, int groupIndex)
	{
		this.selection = selection;
		this.isBackground = isBackground;
		firstCommandId = FirstCommandId + groupIndex * MaximumCommands;
		try
		{
			var folderPath = isBackground ? items[0].FileSystemPath : Path.GetDirectoryName(items[0].FileSystemPath);
			site = folderPath is null ? null : new FolderViewSite(folderPath);
			AddGroup(group);
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not load packaged context menu commands.");
		}

		foreach (var item in Items)
		{
			if (item.SubItems is { } subItems)
				submenus.Add(subItems);
		}
	}

	public bool HasTitle(string? label) => label is not null && titles.Contains(label);

	public bool HasItem(int itemId) => (uint)(itemId - firstCommandId) < MaximumCommands;

	public bool HasSubmenu(List<Win32ContextMenuItem> items) => submenus.Contains(items);

	public HRESULT InvokeItem(int itemId)
	{
		var index = itemId - firstCommandId;
		if ((uint)index >= (uint)commands.Count)
			return HRESULT.E_INVALIDARG;

		// The command state can change while the menu is open
		var (command, commandSelection) = commands[index];
		var result = command.GetState(commandSelection, true, out var state);
		if (result.Failed)
			return result;

		return (state & (_EXPCMDSTATE.ECS_HIDDEN | _EXPCMDSTATE.ECS_DISABLED)) == 0 ? command.Invoke(commandSelection, null) : HRESULT.E_FAIL;
	}

	private void AddGroup(IGrouping<string, ExplorerCommandRegistration> group)
	{
		var groupItems = new List<Win32ContextMenuItem>();
		foreach (var registration in group)
		{
			try
			{
				// For background commands, as in Explorer, app commands receive no selection and get the folder
				// from the site, while cloud provider commands receive the folder directly.
				var commandSelection = isBackground && !registration.IsCloudProvider ? null : selection;

				// The class ID is also the verb of the command in the classic menu
				if (CreateCommand(registration) is { } command)
					AddCommand(command, commandSelection, registration.ClassId.ToString("B").ToUpperInvariant(), groupItems, 0);
			}
			catch (Exception ex)
			{
				Program.Logger.LogDebug(ex, "Could not load packaged context menu command {ClassId}.", registration.ClassId);
			}
		}

		var commandCount = groupItems.Count(item => item.Type != MENU_ITEM_TYPE.MFT_SEPARATOR);
		if (commandCount == 0)
			return;

		var displayName = group.First().DisplayName;
		if (commandCount == 1 || string.IsNullOrEmpty(displayName))
		{
			Items.AddRange(groupItems);
		}
		else
		{
			var groupItem = new ContextMenuItem
			{
				ID = -1,
				Label = displayName,
				SubItems = groupItems,
			};
			Items.Add(groupItem);
			details.Add((groupItem, null, null, group.Select(registration => registration.GroupIcon).FirstOrDefault(icon => !string.IsNullOrEmpty(icon)), 0));
		}
	}

	public async Task LoadDetailsAsync(ThreadWithMessageQueue thread, CancellationToken cancellationToken)
	{
		for (var index = 0; index < details.Count && !cancellationToken.IsCancellationRequested; index++)
		{
			var (item, command, commandSelection, iconLocation, depth) = details[index];

			// Queue each operation separately so invocation can run between detail requests on this STA.
			await thread.PostMethod(() =>
			{
				using var timeout = new ComCallTimeout(ContextMenu.ModernItemsTimeout);
				item.Icon = command is null ? LoadIcon(iconLocation) : GetIcon(command, commandSelection);
			});
			if (command is null || item.SubItems is not { } subItems)
				continue;

			await AddSubCommandsAsync(thread, command, commandSelection, subItems, depth + 1, cancellationToken);

			// Without visible subcommands, the command is invoked itself
			if (!subItems.Any(subItem => subItem.Type != MENU_ITEM_TYPE.MFT_SEPARATOR))
				item.SubItems = null;
		}

		details.Clear();
	}

	private void AddCommand(IExplorerCommand command, IShellItemArray? commandSelection, string commandString, List<Win32ContextMenuItem> target, int depth)
	{
		ownedCommands.Add(command);
		if (commands.Count >= MaximumCommands || depth > MaximumDepth)
			return;

		if (command.GetState(commandSelection, true, out var state).Failed || state.HasFlag(_EXPCMDSTATE.ECS_HIDDEN))
			return;

		if (command.GetFlags(out var flags).Failed)
			flags = _EXPCMDFLAGS.ECF_DEFAULT;

		if (flags.HasFlag(_EXPCMDFLAGS.ECF_ISSEPARATOR))
		{
			target.Add(CreateSeparator());
			return;
		}

		if (command.GetTitle(commandSelection, out var title).Failed || string.IsNullOrEmpty(title))
			return;

		var item = new ContextMenuItem
		{
			ID = firstCommandId + commands.Count,
			Label = title,
			CommandString = commandString,
			IsEnabled = !state.HasFlag(_EXPCMDSTATE.ECS_DISABLED),
			IsChecked = state.HasFlag(_EXPCMDSTATE.ECS_CHECKED),
			IsToggle = (state & (_EXPCMDSTATE.ECS_CHECKBOX | _EXPCMDSTATE.ECS_RADIOCHECK)) != 0 || flags.HasFlag(_EXPCMDFLAGS.ECF_TOGGLEABLE),
			SubItems = flags.HasFlag(_EXPCMDFLAGS.ECF_HASSUBCOMMANDS) ? [] : null,
		};
		commands.Add((command, commandSelection));
		details.Add((item, command, commandSelection, null, depth));
		if (depth == 0)
			titles.Add(title);

		if (flags.HasFlag(_EXPCMDFLAGS.ECF_SEPARATORBEFORE))
			target.Add(CreateSeparator());
		target.Add(item);
		if (flags.HasFlag(_EXPCMDFLAGS.ECF_SEPARATORAFTER))
			target.Add(CreateSeparator());
	}

	private async Task AddSubCommandsAsync(ThreadWithMessageQueue thread, IExplorerCommand parent, IShellItemArray? commandSelection, List<Win32ContextMenuItem> target, int depth, CancellationToken cancellationToken)
	{
		if (depth > MaximumDepth || cancellationToken.IsCancellationRequested)
			return;

		var enumerator = await thread.PostMethod(() =>
		{
			using var timeout = new ComCallTimeout(ContextMenu.ModernItemsTimeout);
			return parent.EnumSubCommands(out var commands).Succeeded ? commands : null;
		});
		if (enumerator is null)
			return;

		try
		{
			var buffer = new IExplorerCommand?[1];
			for (var index = 0; index < MaximumCommands && commands.Count < MaximumCommands && !cancellationToken.IsCancellationRequested; index++)
			{
				var hasNext = await thread.PostMethod(() =>
				{
					using var timeout = new ComCallTimeout(ContextMenu.ModernItemsTimeout);
					if (enumerator.Next(1, buffer, out var fetched).Failed || fetched != 1 || buffer[0] is not { } command)
						return false;

					buffer[0] = null;
					try
					{
						AddCommand(command, commandSelection, string.Empty, target, depth);
					}
					catch (Exception ex)
					{
						Program.Logger.LogDebug(ex, "Could not load a packaged context menu subcommand.");
					}
					return true;
				});
				if (!hasNext)
					break;
			}
		}
		finally
		{
			await thread.PostMethod(() => Release(enumerator));
		}
	}

	private IExplorerCommand? CreateCommand(ExplorerCommandRegistration registration)
	{
		if (ExplorerCommandActivator.Create(registration.ClassId) is not { } command)
			return null;

		if (command is IInitializeCommand initializer && registration.Verb.Length > 0 && initializer.Initialize(registration.Verb, null!).Failed)
		{
			Release(command);
			return null;
		}

		if (site is not null && command is IObjectWithSite objectWithSite)
			objectWithSite.SetSite(site);

		return command;
	}

	private static byte[]? GetIcon(IExplorerCommand command, IShellItemArray? commandSelection)
	{
		try
		{
			return command.GetIcon(commandSelection, out var location).Succeeded ? LoadIcon(location) : null;
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
