// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using WinRT;
using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;

namespace Files.App.UserControls
{
	public sealed partial class ShelfPane : UserControl
	{
		public ShelfPane()
		{
			InitializeComponent();
		}

		private void Shelf_DragOver(object sender, DragEventArgs e)
		{
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			e.Handled = true;
			e.DragUIOverride.Caption = Strings.AddToShelf.GetLocalizedResource();
			e.AcceptedOperation = DataPackageOperation.Link;
		}

		private async void Shelf_Drop(object sender, DragEventArgs e)
		{
			if (ItemsSource is null)
				return;

			// Get items
			var storageService = Ioc.Default.GetRequiredService<IStorageService>();
			var storageItems = (await FilesystemHelpers.GetDraggedStorageItems(e.DataView)).ToArray();

			// Add to list
			foreach (var item in storageItems)
			{
				// Avoid adding duplicates
				if (ItemsSource.Any(x => x.Inner.Id == item.Path))
					continue;

				var storable = item switch
				{
					StorageFileWithPath => (IStorableChild?)await storageService.TryGetFileAsync(item.Path),
					StorageFolderWithPath => (IStorableChild?)await storageService.TryGetFolderAsync(item.Path),
					_ => null
				};

				if (storable is null)
					continue;

				var shelfItem = new ShelfItem(storable, ItemsSource);
				_ = shelfItem.InitAsync();

				ItemsSource.Add(shelfItem);
			}
		}

		private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			var apidl = SafetyExtensions.IgnoreExceptions(() => e.Items
				.Cast<ShelfItem>()
				.Select(x => new ShellItem(x.Inner.Id).PIDL)
				.ToArray());

			if (apidl is null)
				return;

			if (!Shell32.SHGetDesktopFolder(out var pDesktop).Succeeded)
				return;

			if (!Shell32.SHGetIDListFromObject(pDesktop, out var pDesktopPidl).Succeeded)
				return;

			e.Data.Properties["Files_ActionBinder"] = "Files_ShelfBinder";
			if (!Shell32.SHCreateDataObject(pDesktopPidl, apidl, null, out var ppDataObject).Succeeded)
				return;

			var dataObjectProvider = e.Data.As<Shell32.IDataObjectProvider>();
			dataObjectProvider.SetDataObject(ppDataObject);
		}

		public ObservableCollection<ShelfItem>? ItemsSource
		{
			get => (ObservableCollection<ShelfItem>?)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<ShelfItem>), typeof(ShelfPane), new PropertyMetadata(null));

		public ICommand? ClearCommand
		{
			get => (ICommand?)GetValue(ClearCommandProperty);
			set => SetValue(ClearCommandProperty, value);
		}
		public static readonly DependencyProperty ClearCommandProperty =
			DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(ShelfPane), new PropertyMetadata(null));
	}
}
