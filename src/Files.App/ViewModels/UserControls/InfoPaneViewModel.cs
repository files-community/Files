// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.FilePreviews;
using Files.App.ViewModels.Previews;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls
{
	public class InfoPaneViewModel : ObservableObject, IDisposable
	{
		private IInfoPaneSettingsService infoPaneSettingsService { get; } = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();
		private IContentPageContext contentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		private CancellationTokenSource loadCancellationTokenSource;

		/// <summary>
		/// Value indicating if the info pane is on/off
		/// </summary>
		private bool isEnabled;
		public bool IsEnabled
		{
			get => isEnabled;
			set
			{
				infoPaneSettingsService.IsEnabled = value;

				SetProperty(ref isEnabled, value);
			}
		}

		/// <summary>
		/// Current selected item in the file list.
		/// TODO see about removing this and accessing it from the page context instead
		/// </summary>
		private ListedItem selectedItem;
		public ListedItem SelectedItem
		{
			get => selectedItem;
			set
			{
				if (selectedItem is not null)
					selectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

				if (SetProperty(ref selectedItem, value))
				{
					UpdateTagsItems();
					OnPropertyChanged(nameof(LoadTagsList));

					if (value is not null)
						value.PropertyChanged += SelectedItem_PropertyChanged;
				}
			}
		}

		/// <summary>
		/// Enum indicating whether to show the details or preview tab
		/// </summary>
		public InfoPaneTabs SelectedTab
		{
			get => infoPaneSettingsService.SelectedTab;
			set
			{
				if (value != infoPaneSettingsService.SelectedTab)
				{
					infoPaneSettingsService.SelectedTab = value;
				}
			}
		}

		/// <summary>
		/// Enum indicating if details/preview are available
		/// </summary>
		private PreviewPaneStates previewPaneState;
		public PreviewPaneStates PreviewPaneState
		{
			get => previewPaneState;
			set
			{
				if (SetProperty(ref previewPaneState, value))
					OnPropertyChanged(nameof(LoadTagsList));
			}
		}

		/// <summary>
		/// Value indicating if the download cloud files option should be displayed
		/// </summary>
		private bool showCloudItemButton;
		public bool ShowCloudItemButton
		{
			get => showCloudItemButton;
			set => SetProperty(ref showCloudItemButton, value);
		}

		private UIElement previewPaneContent;
		public UIElement PreviewPaneContent
		{
			get => previewPaneContent;
			set => SetProperty(ref previewPaneContent, value);
		}

		public bool LoadTagsList
			=> SelectedItem?.HasTags ?? false &&
			PreviewPaneState is PreviewPaneStates.NoPreviewAvailable ||
			PreviewPaneState is PreviewPaneStates.PreviewAndDetailsAvailable;

		public ObservableCollection<TagsListItem> Items { get; } = new();

		public InfoPaneViewModel()
		{
			infoPaneSettingsService.PropertyChanged += PreviewSettingsService_OnPropertyChangedEvent;
			contentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;

			IsEnabled = infoPaneSettingsService.IsEnabled;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItem):

					if (contentPageContext.SelectedItems.Count == 1)
						SelectedItem = contentPageContext.SelectedItems.First();
					else
						SelectedItem = null;

					var shouldUpdatePreview = ((MainWindow.Instance.Content as Frame)?.Content as MainPage)?.ShouldPreviewPaneBeActive;
					if (shouldUpdatePreview == true)
						_ = UpdateSelectedItemPreviewAsync();
					break;
			}
		}

		private async Task LoadPreviewControlAsync(CancellationToken token, bool downloadItem)
		{
			if (SelectedItem.IsHiddenItem && !SelectedItem.ItemPath.EndsWith("\\"))
			{
				PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;

				PreviewPaneContent = null;
				return;
			}

			var control = await GetBuiltInPreviewControlAsync(SelectedItem, downloadItem);

			if (token.IsCancellationRequested)
				return;

			if (control is not null)
			{
				PreviewPaneContent = control;
				PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
				return;
			}

			var basicModel = new BasicPreviewViewModel(SelectedItem);
			await basicModel.LoadAsync();

			control = new BasicPreview(basicModel);

			if (token.IsCancellationRequested)
				return;

			PreviewPaneContent = control;
			PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
		}

		private async Task<UserControl> GetBuiltInPreviewControlAsync(ListedItem item, bool downloadItem)
		{
			ShowCloudItemButton = false;

			if (item.IsRecycleBinItem)
			{
				if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsArchive)
				{
					var model = new FolderPreviewViewModel(item);
					await model.LoadAsync();

					return new FolderPreview(model);
				}
				else
				{
					var model = new BasicPreviewViewModel(item);
					await model.LoadAsync();

					return new BasicPreview(model);
				}
			}

			if (item.IsShortcut)
			{
				var model = new ShortcutPreviewViewModel(item);
				await model.LoadAsync();

				return new BasicPreview(model);
			}

			if (FileExtensionHelpers.IsBrowsableZipFile(item.FileExtension, out _))
			{
				var model = new ArchivePreviewViewModel(item);
				await model.LoadAsync();

				return new BasicPreview(model);
			}

			if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
			{
				var model = new FolderPreviewViewModel(item);
				await model.LoadAsync();

				if (contentPageContext.SelectedItems.Count == 0)
					item.FileTags ??= FileTagsHelper.ReadFileTag(item.ItemPath);

				return new FolderPreview(model);
			}

			if (item.FileExtension is null)
				return null;

			if (item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline && !downloadItem)
			{
				ShowCloudItemButton = true;

				return null;
			}

			var ext = item.FileExtension.ToLowerInvariant();

			if (!item.IsFtpItem &&
				contentPageContext.PageType != ContentPageTypes.ZipFolder &&
				(FileExtensionHelpers.IsAudioFile(ext) || FileExtensionHelpers.IsVideoFile(ext)))
			{
				var model = new MediaPreviewViewModel(item);
				await model.LoadAsync();

				return new MediaPreview(model);
			}

			if (MarkdownPreviewViewModel.ContainsExtension(ext))
			{
				var model = new MarkdownPreviewViewModel(item);
				await model.LoadAsync();

				return new MarkdownPreview(model);
			}

			if (ImagePreviewViewModel.ContainsExtension(ext))
			{
				var model = new ImagePreviewViewModel(item);
				await model.LoadAsync();

				return new ImagePreview(model);
			}

			if (TextPreviewViewModel.ContainsExtension(ext))
			{
				var model = new TextPreviewViewModel(item);
				await model.LoadAsync();

				return new TextPreview(model);
			}

			/*if (PDFPreviewViewModel.ContainsExtension(ext))
			{
				var model = new PDFPreviewViewModel(item);
				await model.LoadAsync();

				return new PDFPreview(model);
			}*/

			/*if (HtmlPreviewViewModel.ContainsExtension(ext))
			{
				var model = new HtmlPreviewViewModel(item);
				await model.LoadAsync();

				return new HtmlPreview(model);
			}*/

			if (RichTextPreviewViewModel.ContainsExtension(ext))
			{
				var model = new RichTextPreviewViewModel(item);
				await model.LoadAsync();

				return new RichTextPreview(model);
			}

			if (CodePreviewViewModel.ContainsExtension(ext))
			{
				var model = new CodePreviewViewModel(item);
				await model.LoadAsync();

				return new CodePreview(model);
			}

			if
			(
				ShellPreviewViewModel.FindPreviewHandlerFor(item.FileExtension, 0) is not null &&
				!FileExtensionHelpers.IsFontFile(item.FileExtension)
			)
			{
				var model = new ShellPreviewViewModel(item);
				await model.LoadAsync();

				return new ShellPreview(model);
			}

			var control = await TextPreviewViewModel.TryLoadAsTextAsync(item);

			return control ?? null;
		}

		public async Task UpdateSelectedItemPreviewAsync(bool downloadItem = false)
		{
			loadCancellationTokenSource?.Cancel();
			if (SelectedItem is not null && contentPageContext.SelectedItems.Count == 1)
			{
				SelectedItem?.FileDetails?.Clear();

				try
				{
					PreviewPaneState = PreviewPaneStates.LoadingPreview;

					if (SelectedTab == InfoPaneTabs.Preview ||
						SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder)
					{
						loadCancellationTokenSource = new CancellationTokenSource();
						await LoadPreviewControlAsync(loadCancellationTokenSource.Token, downloadItem);
					}
					else
					{
						await LoadBasicPreviewAsync();
						return;
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
					loadCancellationTokenSource?.Cancel();

					// If initial loading fails, attempt to load a basic preview (thumbnail and details only)
					// If that fails, revert to no preview/details available as long as the item is not a shortcut or folder
					if (SelectedItem is not null && !SelectedItem.IsShortcut && SelectedItem.PrimaryItemAttribute != StorageItemTypes.Folder)
					{
						await LoadBasicPreviewAsync();
						return;
					}

					PreviewPaneContent = null;
					PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;
				}
			}
			else if (contentPageContext.SelectedItems.Count > 0)
			{
				PreviewPaneContent = null;
				PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;
			}
			else
			{
				SelectedItem?.FileDetails?.Clear();
				var currentFolder = contentPageContext.Folder;

				if (currentFolder is null)
				{
					PreviewPaneContent = null;
					PreviewPaneState = PreviewPaneStates.NoItemSelected;
					return;
				}

				try
				{
					PreviewPaneState = PreviewPaneStates.LoadingPreview;
					loadCancellationTokenSource = new CancellationTokenSource();

					SelectedItem = currentFolder;
					await LoadPreviewControlAsync(loadCancellationTokenSource.Token, downloadItem);
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
					loadCancellationTokenSource?.Cancel();

					PreviewPaneContent = null;
					PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;
				}
			}
		}

		public void UpdateDateDisplay()
		{
			SelectedItem?.FileDetails?.ForEach(property =>
			{
				if (property.Value is DateTimeOffset)
					property.UpdateValueText();
			});
		}

		private async void PreviewSettingsService_OnPropertyChangedEvent(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(infoPaneSettingsService.SelectedTab))
			{
				OnPropertyChanged(nameof(SelectedTab));

				// The preview will need refreshing as the file details won't be accurate
				await UpdateSelectedItemPreviewAsync();
			}
			else if (e.PropertyName is nameof(infoPaneSettingsService.IsEnabled))
			{
				var newEnablingStatus = infoPaneSettingsService.IsEnabled;
				if (isEnabled != newEnablingStatus)
				{
					isEnabled = newEnablingStatus;
					_ = UpdateSelectedItemPreviewAsync();
					OnPropertyChanged(nameof(IsEnabled));
				}
			}
		}

		private async Task LoadBasicPreviewAsync()
		{
			try
			{
				var basicModel = new BasicPreviewViewModel(SelectedItem);
				await basicModel.LoadAsync();

				PreviewPaneContent = new BasicPreview(basicModel);
				PreviewPaneState = PreviewPaneStates.PreviewAndDetailsAvailable;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		private void SelectedItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ListedItem.HasTags))
				OnPropertyChanged(nameof(LoadTagsList));
			else if (e.PropertyName is nameof(ListedItem.FileTagsUI))
				UpdateTagsItems();
		}

		private void UpdateTagsItems()
		{
			Items.Clear();

			SelectedItem?.FileTagsUI?.ForEach(tag => Items.Add(new TagItem(tag)));

			Items.Add(new FlyoutItem(new Files.App.UserControls.Menus.FileTagsContextMenu(new List<ListedItem>() { SelectedItem })));
		}

		public void Dispose()
		{

		}
	}
}
