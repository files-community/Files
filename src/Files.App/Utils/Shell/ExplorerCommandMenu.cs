// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents an item of the Windows 11 context menu, backed by an <see cref="IExplorerCommand"/>.
	/// </summary>
	public sealed class ExplorerCommandMenuItem : Win32ContextMenuItem
	{
		internal IExplorerCommand? Command { get; init; }

		/// <summary>
		/// The worker the command was created on, which must also invoke it.
		/// </summary>
		internal ExplorerCommandMenu.Lane? Lane { get; init; }

		/// <summary>
		/// The class registered in the package manifest; set on top-level items only.
		/// </summary>
		public Guid Clsid { get; internal set; }

		public bool IsEnabled { get; init; } = true;
	}

	/// <summary>
	/// Provides the Windows 11 context menu commands that packaged apps register
	/// (<c>windows.fileExplorerContextMenus</c>), which the classic shell context menu does not include.
	/// </summary>
	public sealed partial class ExplorerCommandMenu : IDisposable
	{
		private const int MaxDepth = 4;

		// Each command runs in its own server process: query them on several workers, so that a slow one
		// does not hold up the others.
		private const int MaxLanes = 4;

		// Each command runs out of process; give up on the whole menu if one of them hangs.
		private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(5);

		private readonly List<Lane> lanes = [];
		private bool disposedValue;

		public List<ExplorerCommandMenuItem> Items { get; } = [];

		/// <summary>
		/// A worker thread with the COM objects created on it.
		/// </summary>
		internal sealed class Lane(ContextMenuWorkerPool.Worker worker)
		{
			public ContextMenuWorkerPool.Worker Worker { get; } = worker;

			public List<object> ComObjects { get; } = [];

			public IShellItemArray? ItemArray { get; set; }

			public ExplorerCommandSite? Site { get; set; }
		}

		private ExplorerCommandMenu()
		{
		}

		/// <param name="folderPath">The folder the menu was opened in, which the commands get through their site.</param>
		public static async Task<ExplorerCommandMenu?> GetExplorerCommandMenuAsync(string[] filePaths, bool isBackground, string folderPath, Func<string?, bool>? itemFilter, CancellationToken cancellationToken)
		{
			var verbs = await ExplorerCommandCatalog.GetVerbsAsync();
			if (verbs.Count is 0 || filePaths.Length is 0 || cancellationToken.IsCancellationRequested)
				return null;

			var menu = new ExplorerCommandMenu();
			var load = menu.LoadAsync(filePaths, isBackground, folderPath, verbs, itemFilter, cancellationToken);
			if (await Task.WhenAny(load, Task.Delay(QueryTimeout, cancellationToken)) != load)
			{
				// The workers stay out of the pool until the hanging calls return.
				_ = load.ContinueWith(_ => menu.Dispose(), TaskScheduler.Default);
				return null;
			}

			if (!await load)
			{
				menu.Dispose();
				return null;
			}

			return menu;
		}

		/// <summary>
		/// Whether a classic context menu item is the shell's own copy of one of these commands.
		/// </summary>
		public bool IsDuplicate(Win32ContextMenuItem classicItem)
		{
			if (classicItem.Type is not MENU_ITEM_TYPE.MFT_STRING || classicItem.CommandString is "openas" or "sendto")
				return false;

			// The shell gives some of them the command's class as verb; others are only recognizable by their title.
			if (Guid.TryParse(classicItem.CommandString, out var clsid) && Items.Any(x => x.Clsid == clsid))
				return true;

			var label = RemoveAccessKeys(classicItem.Label);
			return !string.IsNullOrEmpty(label) && Items.Any(x => string.Equals(RemoveAccessKeys(x.Label), label, StringComparison.Ordinal));
		}

		public async Task<bool> InvokeItem(ExplorerCommandMenuItem item)
		{
			if (item.Command is not { } command || item.Lane is not { } lane || disposedValue)
				return false;

			try
			{
				var currentWindows = Win32Helper.GetDesktopWindows();
				HRESULT result = await lane.Worker.Thread.PostMethod(() => command.Invoke(lane.ItemArray, null));
				if (result.Failed)
					return false;
				Win32Helper.BringToForeground(currentWindows);
				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
				return false;
			}
		}

		private async Task<bool> LoadAsync(string[] filePaths, bool isBackground, string folderPath, IReadOnlyList<ExplorerCommandCatalog.Verb> verbs, Func<string?, bool>? itemFilter, CancellationToken cancellationToken)
		{
			try
			{
				var firstLane = AddLane();
				var clsids = await firstLane.Worker.Thread.PostMethod(() => GetApplicableCommands(firstLane, filePaths, isBackground, verbs, itemFilter));
				if (clsids.Count is 0 || cancellationToken.IsCancellationRequested)
					return false;

				// Every worker takes the next command from a shared queue; the results keep the catalog's order.
				var queue = new ConcurrentQueue<(int Index, Guid Clsid)>(clsids.Select((clsid, index) => (index, clsid)));
				var results = new ExplorerCommandMenuItem?[clsids.Count];
				var queryLanes = new List<Lane> { firstLane };
				for (int index = 1; index < Math.Min(MaxLanes, clsids.Count); index++)
					queryLanes.Add(AddLane());

				await Task.WhenAll(queryLanes.Select(lane => lane.Worker.Thread.PostMethod(() => QueryCommands(lane, filePaths, folderPath, queue, results, itemFilter))));

				Items.AddRange(results.WhereNotNull());
				return Items.Count > 0;
			}
			catch (Exception ex)
			{
				// A faulty handler must not take the classic shell menu down with it.
				Debug.WriteLine(ex);
				return false;
			}
		}

		private Lane AddLane()
		{
			lock (lanes)
			{
				var lane = new Lane(ContextMenuWorkerPool.Rent());
				lanes.Add(lane);
				return lane;
			}
		}

		private static List<Guid> GetApplicableCommands(Lane lane, string[] filePaths, bool isBackground, IReadOnlyList<ExplorerCommandCatalog.Verb> verbs, Func<string?, bool>? itemFilter)
		{
			var shellItems = GetShellItems(filePaths);
			try
			{
				if (shellItems.Count is 0)
					return [];

				var selection = shellItems.Select(item => GetSelectedItem(item, isBackground)).ToList();
				lane.ItemArray = ContextMenu.CreateShellItemArray([.. shellItems]);

				// A command applies when each selected item matches one of the types it is registered for.
				return verbs
					.GroupBy(verb => verb.Clsid)
					.Where(group => selection.All(item => group.Any(verb => Matches(verb.ItemType, item))))
					.Select(group => group.Key)
					.Where(clsid => itemFilter?.Invoke(clsid.ToString("B").ToUpperInvariant()) is not true)
					.ToList();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return [];
			}
			finally
			{
				foreach (var item in shellItems)
					item.Dispose();
			}
		}

		private static void QueryCommands(Lane lane, string[] filePaths, string folderPath, ConcurrentQueue<(int Index, Guid Clsid)> queue, ExplorerCommandMenuItem?[] results, Func<string?, bool>? itemFilter)
		{
			try
			{
				lane.Site = ExplorerCommandSite.Create(folderPath);

				if (lane.ItemArray is null)
				{
					var shellItems = GetShellItems(filePaths);
					try
					{
						lane.ItemArray = ContextMenu.CreateShellItemArray([.. shellItems]);
					}
					finally
					{
						foreach (var item in shellItems)
							item.Dispose();
					}
				}

				while (queue.TryDequeue(out var entry))
				{
					if (CreateCommand(lane, entry.Clsid) is { } command && Describe(lane, command, itemFilter, 0) is { } item)
					{
						item.Clsid = entry.Clsid;
						results[entry.Index] = item;
					}
				}
			}
			catch (Exception ex)
			{
				// The other workers go on with the remaining commands.
				Debug.WriteLine(ex);
			}
		}

		private static List<ShellItem> GetShellItems(string[] filePaths)
		{
			var shellItems = new List<ShellItem>();
			try
			{
				foreach (string path in filePaths.Where(path => !string.IsNullOrEmpty(path)))
					shellItems.Add(ShellFolderExtensions.GetShellItemFromPathOrPIDL(path));
				return shellItems;
			}
			catch
			{
				foreach (var item in shellItems)
					item.Dispose();
				throw;
			}
		}

		private static IExplorerCommand? CreateCommand(Lane lane, Guid clsid)
		{
			// Activate out of process only, so a third-party handler cannot load into Files.
			HRESULT result = PInvoke.CoCreateInstance(clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IExplorerCommand? command);
			if (result.Failed || command is null)
				return null;

			lane.ComObjects.Add(command);

			// As File Explorer does, before anything else: some commands capture the site when they enumerate
			// their subcommands.
			if (lane.Site is not null && command is IObjectWithSite objectWithSite)
				objectWithSite.SetSite(lane.Site);

			return command;
		}

		private static ExplorerCommandMenuItem? Describe(Lane lane, IExplorerCommand command, Func<string?, bool>? itemFilter, int depth)
		{
			try
			{
				if (command.GetState(lane.ItemArray, true, out var state).Failed || state.HasFlag(_EXPCMDSTATE.ECS_HIDDEN))
					return null;

				if (command.GetFlags(out var flags).Failed)
					flags = _EXPCMDFLAGS.ECF_DEFAULT;

				if (flags.HasFlag(_EXPCMDFLAGS.ECF_ISSEPARATOR))
					return new() { Type = MENU_ITEM_TYPE.MFT_SEPARATOR };

				var title = TakeString(command.GetTitle(lane.ItemArray, out var titlePointer), titlePointer);
				if (string.IsNullOrWhiteSpace(title) || itemFilter?.Invoke(title) is true)
					return null;

				var item = new ExplorerCommandMenuItem
				{
					Type = MENU_ITEM_TYPE.MFT_STRING,
					Label = title,
					Command = command,
					Lane = lane,
					IsEnabled = !state.HasFlag(_EXPCMDSTATE.ECS_DISABLED),
					Icon = GetIcon(TakeString(command.GetIcon(lane.ItemArray, out var iconPointer), iconPointer)),
				};

				if (flags.HasFlag(_EXPCMDFLAGS.ECF_HASSUBCOMMANDS))
				{
					item.SubItems = depth < MaxDepth ? DescribeSubCommands(lane, command, itemFilter, depth) : [];
					if (!item.SubItems.Any(x => x.Type is MENU_ITEM_TYPE.MFT_STRING))
						return null;
				}

				return item;
			}
			catch (Exception ex)
			{
				// Skip just this command.
				Debug.WriteLine(ex);
				return null;
			}
		}

		private static List<Win32ContextMenuItem> DescribeSubCommands(Lane lane, IExplorerCommand command, Func<string?, bool>? itemFilter, int depth)
		{
			var subItems = new List<Win32ContextMenuItem>();
			if (command.EnumSubCommands(out var enumerator).Failed || enumerator is null)
				return subItems;

			lane.ComObjects.Add(enumerator);
			while (NextCommand(enumerator) is { } subCommand)
			{
				lane.ComObjects.Add(subCommand);
				if (Describe(lane, subCommand, itemFilter, depth + 1) is { } subItem)
					subItems.Add(subItem);
			}

			// Drop leading, trailing and repeated separators left by hidden commands.
			for (int index = subItems.Count - 1; index >= 0; index--)
			{
				bool isSeparator = subItems[index].Type is MENU_ITEM_TYPE.MFT_SEPARATOR;
				if (isSeparator && (index == 0 || index == subItems.Count - 1 || subItems[index - 1].Type is MENU_ITEM_TYPE.MFT_SEPARATOR))
					subItems.RemoveAt(index);
			}

			return subItems;
		}

		private static unsafe IExplorerCommand? NextCommand(IEnumExplorerCommand enumerator)
		{
			var commands = new IExplorerCommand[1];
			uint fetched = 0;
			return enumerator.Next(1, commands, &fetched).Succeeded && fetched is 1 ? commands[0] : null;
		}

		private static unsafe string? TakeString(HRESULT result, PWSTR value)
		{
			if (value.Value is null)
				return null;

			try
			{
				return result.Succeeded ? value.ToString() : null;
			}
			finally
			{
				PInvoke.CoTaskMemFree(value.Value);
			}
		}

		private static byte[]? GetIcon(string? location)
		{
			// Indirect strings (@{...}) point to package resources, which are not supported here.
			if (string.IsNullOrWhiteSpace(location) || location.StartsWith('@'))
				return null;

			var path = location;
			var index = 0;
			var comma = location.LastIndexOf(',');
			if (comma > 0 && int.TryParse(location.AsSpan(comma + 1), out var parsedIndex))
			{
				path = location[..comma];
				index = parsedIndex;
			}

			path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));

			// ExtractSelectedIconsFromDLL negates the index it is given.
			return Win32Helper.ExtractSelectedIconsFromDLL(path, [-index], 32).FirstOrDefault()?.IconData;
		}

		private static string? RemoveAccessKeys(string? label)
			=> label?.Replace("&&", "\0").Replace("&", string.Empty).Replace("\0", "&");

		private readonly record struct SelectedItem(bool IsBackground, bool IsFolder, bool IsDrive, string Extension);

		private static SelectedItem GetSelectedItem(ShellItem item, bool isBackground)
		{
			var path = item.FileSystemPath ?? item.ParsingName ?? string.Empty;
			var isFolder = item.IsFolder && !item.IsStream;
			var isDrive = isFolder && path.Length is 2 or 3 && path[1] is ':';
			return new(isBackground, isFolder, isDrive, isFolder ? string.Empty : Path.GetExtension(path));
		}

		private static bool Matches(string itemType, SelectedItem item)
		{
			if (item.IsBackground)
				return itemType.Equals("Directory\\Background", StringComparison.OrdinalIgnoreCase);

			return itemType.ToLowerInvariant() switch
			{
				"*" => !item.IsFolder,
				"directory" => item.IsFolder && !item.IsDrive,
				"drive" => item.IsDrive,
				"folder" => item.IsFolder,
				_ => !item.IsFolder && itemType.StartsWith('.') && itemType.Equals(item.Extension, StringComparison.OrdinalIgnoreCase),
			};
		}

		private void Dispose(bool disposing)
		{
			if (disposedValue)
				return;

			// Release the COM objects on each worker's own STA thread, then give the worker back.
			lock (lanes)
			{
				foreach (var lane in lanes)
				{
					var objectsToRelease = lane.ComObjects.ToArray();
					var itemArrayToRelease = lane.ItemArray;
					var siteToRelease = lane.Site;
					lane.ComObjects.Clear();
					lane.ItemArray = null;
					lane.Site = null;
					lane.Worker.Thread.PostMethod(() =>
					{
						foreach (var comObject in objectsToRelease)
							(comObject as ComObject)?.FinalRelease();
						((object?)itemArrayToRelease as ComObject)?.FinalRelease();
						siteToRelease?.Release();
					});
					ContextMenuWorkerPool.Return(lane.Worker);
				}

				lanes.Clear();
			}

			disposedValue = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ExplorerCommandMenu()
		{
			Dispose(false);
		}
	}
}
