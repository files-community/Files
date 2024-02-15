// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items = [];

		public string WidgetName => nameof(QuickAccessWidgetViewModel);
		public string AutomationProperties => "QuickAccess".GetLocalizedResource();
		public string WidgetHeader => "QuickAccess".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			_ = InitializeWidget();

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItemAsync;
			Items.CollectionChanged += ItemsAdded_CollectionChanged;
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public async Task OpenFileLocation(string path)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.GetLayoutType(path)!,
				new() { NavPathParam = path });
		}

		private async Task InitializeWidget()
		{
			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync();

			ModifyItemAsync(this, new(itemsToAdd.ToArray(), false) { Reset = true });
		}

		private async void ModifyItemAsync(object? sender, ModifyQuickAccessEventArgs? e)
		{
			if (e is null || e.Paths is null || e.Items is null)
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				if (e.Reset)
				{
					// Find the intersection between the two lists and determine whether to remove or add
					var originalItemsAdded = Items.ToList();

					var itemsToRemove = originalItemsAdded.Where(x => !e.Paths.Contains(x.Path));
					var itemsToAdd = e.Paths.Where(x => !originalItemsAdded.Any(y => y.Path == x));

					// Remove items
					foreach (var itemToRemove in itemsToRemove)
						Items.Remove(itemToRemove);

					// Add items
					foreach (var itemToAdd in itemsToAdd)
					{
						var interimItemsAdded = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);

						var occurrence = interimItemsAdded.FirstOrDefault(x => !x.IsPinned);
						var lastIndex = occurrence is null ? -1 : Items.IndexOf(occurrence);

						var isPinned = (bool?)e.Items.Where(x => x.FilePath == itemToAdd).FirstOrDefault()?.Properties["System.Home.IsPinned"] ?? false;
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						Items.Insert(isPinned && lastIndex >= 0 ? Math.Min(lastIndex, Items.Count) : Items.Count, new WidgetFolderCardItem(item, SystemIO.Path.GetFileName(item.Text), isPinned)
						{
							Path = item.Path,
						});
					}

					return;
				}
				if (e.Reorder)
				{
					// Remove pinned items
					foreach (var itemToRemove in Items.ToList().Where(x => x.IsPinned))
						Items.Remove(itemToRemove);

					// Add pinned items in the new order
					foreach (var itemToAdd in e.Paths)
					{
						var interimItemsAdded = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);

						if (interimItemsAdded.FirstOrDefault(x => !x.IsPinned) is not WidgetFolderCardItem cardItem)
							continue;

						var lastIndex = Items.IndexOf(cardItem);
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						Items.Insert(lastIndex >= 0 ? Math.Min(lastIndex, Items.Count) : Items.Count, new WidgetFolderCardItem(item, SystemIO.Path.GetFileName(item.Text), true)
						{
							Path = item.Path,
						});
					}

					return;
				}
				if (e.Add)
				{
					foreach (var itemToAdd in e.Paths)
					{
						var interimItemsAdded = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);

						if (interimItemsAdded.FirstOrDefault(x => !x.IsPinned) is not WidgetFolderCardItem cardItem)
							continue;

						var lastIndex = Items.IndexOf(cardItem);
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;
						Items.Insert(e.Pin && lastIndex >= 0 ? Math.Min(lastIndex, Items.Count) : Items.Count, new WidgetFolderCardItem(item, SystemIO.Path.GetFileName(item.Text), e.Pin) // Add just after the Recent Folders
						{
							Path = item.Path,
						});
					}
				}
				else
				{
					foreach (var itemToRemove in Items.ToList().Where(x => e.Paths.Contains(x.Path)))
						Items.Remove(itemToRemove);
				}
			});
		}

		public override List<ContextMenuFlyoutItemViewModel> GenerateContextFlyoutModel(bool isFolder = false)
		{
			return WidgetQuickAccessItemContextFlyoutFactory.Generate();
		}

		// Event methods

		private static async void ItemsAdded_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetFolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		// Disposer

		public void Dispose()
		{
			App.QuickAccessManager.UpdateQuickAccessWidget -= ModifyItemAsync;
		}
	}
}
