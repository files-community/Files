// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;

namespace Files.App.ViewModels.UserControls
{
	/// <summary>
	/// Represents view model of <see cref="UserControls.BookmarksBar"/>.
	/// </summary>
	public sealed partial class BookmarksBarViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		/// <summary>
		/// Gets the pinned folders shown in the bookmarks bar.
		/// Mirrors the sidebar pinned section so both stay in sync.
		/// </summary>
		public ObservableCollection<LocationItem> Items { get; } = [];

		public bool ShowBookmarksBar => UserSettingsService.AppearanceSettingsService.ShowBookmarksBar;

		public BookmarksBarViewModel()
		{
			UserSettingsService.AppearanceSettingsService.PropertyChanged += AppearanceSettingsService_PropertyChanged;
			App.QuickAccessManager.Model.DataChanged += OnPinnedFoldersChanged;

			_ = RefreshAsync();
		}

		private void AppearanceSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IAppearanceSettingsService.ShowBookmarksBar))
				OnPropertyChanged(nameof(ShowBookmarksBar));
		}

		private async void OnPinnedFoldersChanged(object? sender, NotifyCollectionChangedEventArgs e)
			=> await RefreshAsync();

		/// <summary>
		/// Rebuilds the bar from the pinned folder items, skipping the built-in locations
		/// (Home, Recycle Bin, drives) that are not user bookmarks.
		/// </summary>
		public async Task RefreshAsync()
		{
			if (MainWindow.Instance is not { } mainWindow)
				return;

			await mainWindow.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				var pinnedItems = App.QuickAccessManager.Model.PinnedFolderItems
					.OfType<LocationItem>()
					.Where(item => !item.IsDefaultLocation && !string.IsNullOrEmpty(item.Path))
					.ToList();

				Items.Clear();

				foreach (var item in pinnedItems)
					Items.Add(item);
			});
		}

		public void Dispose()
		{
			UserSettingsService.AppearanceSettingsService.PropertyChanged -= AppearanceSettingsService_PropertyChanged;
			App.QuickAccessManager.Model.DataChanged -= OnPinnedFoldersChanged;
		}
	}
}
