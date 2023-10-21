// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.FilePreviews;
using Files.App.ViewModels.Previews;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls
{
	public class PreviewPaneViewModel : ObservableObject, IDisposable
	{
		private readonly IPreviewPaneSettingsService previewSettingsService;

		private readonly IContentPageContext contentPageContextService;

		private CancellationTokenSource loadCancellationTokenSource;

		private bool isEnabled;
		public bool IsEnabled
		{
			get => isEnabled;
			set
			{
				previewSettingsService.IsEnabled = value;

				SetProperty(ref isEnabled, value);
			}
		}

		private bool isItemSelected;
		public bool IsItemSelected
		{
			get => isItemSelected;
			set => SetProperty(ref isItemSelected, value);
		}

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

		public PreviewPaneViewModel(IPreviewPaneSettingsService previewSettings, IContentPageContext contentPageContextService = null)
		{
			previewSettingsService = previewSettings;

			ShowPreviewOnlyInvoked = new RelayCommand(async () => await UpdateSelectedItemPreviewAsync());

			IsEnabled = previewSettingsService.IsEnabled;

			previewSettingsService.PropertyChanged += PreviewSettingsService_OnPropertyChangedEventAsync;

			this.contentPageContextService = contentPageContextService ?? Ioc.Default.GetRequiredService<IContentPageContext>();
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

			if (SelectedItem.IsRecycleBinItem)
			{
				if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsArchive)
				{
					var model = new FolderPreviewViewModel(item);
					await model.LoadAsync();

					return new FolderPreview(model);
				}
				else
				{
					var model = new BasicPreviewViewModel(SelectedItem);
					await model.LoadAsync();

					return new BasicPreview(model);
				}
			}

			if (item.IsShortcut)
			{
				var model = new ShortcutPreviewViewModel(SelectedItem);
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

				if (!isItemSelected)
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

			if (MediaPreviewViewModel.ContainsExtension(ext))
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
			if (SelectedItem is not null && IsItemSelected)
			{
				SelectedItem?.FileDetails?.Clear();

				try
				{
					PreviewPaneState = PreviewPaneStates.LoadingPreview;
					loadCancellationTokenSource = new CancellationTokenSource();
					await LoadPreviewControlAsync(loadCancellationTokenSource.Token, downloadItem);
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
			else if (IsItemSelected)
			{
				PreviewPaneContent = null;
				PreviewPaneState = PreviewPaneStates.NoPreviewOrDetailsAvailable;
			}
			else
			{
				SelectedItem?.FileDetails?.Clear();
				var currentFolder = contentPageContextService.Folder;

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
			SelectedItem?.FileDetails?.ForEach(property => {
				if (property.Value is DateTimeOffset)
					property.UpdateValueText();
			});
		}

		public ICommand ShowPreviewOnlyInvoked { get; }

		private async void PreviewSettingsService_OnPropertyChangedEventAsync(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IPreviewPaneSettingsService.ShowPreviewOnly))
			{
				// The preview will need refreshing as the file details won't be accurate
				await UpdateSelectedItemPreviewAsync();
			}
			else if (e.PropertyName is nameof(IPreviewPaneSettingsService.IsEnabled))
			{
				var newEnablingStatus = previewSettingsService.IsEnabled;
				if (isEnabled != newEnablingStatus)
				{
					isEnabled = newEnablingStatus;
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
			previewSettingsService.PropertyChanged -= PreviewSettingsService_OnPropertyChangedEventAsync;
		}
	}
}
