// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models;

public sealed partial class ShellContextMenuResult : IDisposable
{
	private readonly ContextMenu? menu;

	public static ShellContextMenuResult Empty
		=> new(null, [], Task.FromResult<List<ContextMenuFlyoutItemViewModel>>([]));

	public List<ContextMenuFlyoutItemViewModel> ModernItems { get; }

	public Task<List<ContextMenuFlyoutItemViewModel>> ClassicItems { get; }

	internal ShellContextMenuResult(ContextMenu? menu, List<ContextMenuFlyoutItemViewModel> modernItems, Task<List<ContextMenuFlyoutItemViewModel>> classicItems)
	{
		this.menu = menu;
		ModernItems = modernItems;
		ClassicItems = classicItems;
	}

	public void Dispose()
		=> menu?.Dispose();
}
