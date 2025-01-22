// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Vanara.Windows.Shell;
using WinRT;
using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;

namespace Files.App.UserControls
{
	public sealed partial class ShelfPane : UserControl
	{
		public ShelfPane()
		{
			// TODO: [Shelf] Remove once view model is connected
			ItemsSource = new ObservableCollection<ShelfItem>();

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
					StorageFileWithPath => (IStorable?)await storageService.TryGetFileAsync(item.Path),
					StorageFolderWithPath => (IStorable?)await storageService.TryGetFolderAsync(item.Path),
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

            if (!Shell32.SHCreateDataObject(null, apidl, null, out var ppDataObject).Succeeded)
	            return;

            e.Data.Properties["Files_ActionBinder"] = "Files_ShelfBinder";
			ppDataObject.SetData(StandardDataFormats.StorageItems, apidl);
			var dataObjectProvider = e.Data.As<Shell32.IDataObjectProvider>();
			dataObjectProvider.SetDataObject(ppDataObject);


            //var obj = new ShellDataObject();
            //ppDataObject.SetData(StandardDataFormats.StorageItems, obj);
		}

		public IList<ShelfItem>? ItemsSource
		{
			get => (IList<ShelfItem>?)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(IList<ShelfItem>), typeof(ShelfPane), new PropertyMetadata(null));

		public ICommand? ClearCommand
		{
			get => (ICommand?)GetValue(ClearCommandProperty);
			set => SetValue(ClearCommandProperty, value);
		}
		public static readonly DependencyProperty ClearCommandProperty =
			DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(ShelfPane), new PropertyMetadata(null));
	}
}
