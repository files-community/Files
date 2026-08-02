// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.ViewModels.Previews
{
	public abstract partial class BasePreviewModel : ObservableObject
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public ListedItem Item { get; }

		protected BaseStorageFile PreviewFile
			=> Item.ItemFile ?? throw new InvalidOperationException("The preview file has not been loaded.");

		private BitmapImage? fileImage;
		public BitmapImage? FileImage
		{
			get => fileImage;
			protected set => SetProperty(ref fileImage, value);
		}

		public List<FileProperty>? DetailsFromPreview { get; set; }

		/// <summary>
		/// This is cancelled when the user has selected another file or closed the pane.
		/// </summary>
		public CancellationTokenSource LoadCancelledTokenSource { get; } = new CancellationTokenSource();

		public BasePreviewModel(ListedItem item) : base()
			=> Item = item;

		public delegate void LoadedEventHandler(object? sender, EventArgs e);

		public static Task LoadDetailsOnlyAsync(ListedItem item, List<FileProperty>? details = null)
		{
			var temp = new DetailsOnlyPreviewModel(item) { DetailsFromPreview = details };
			return temp.LoadAsync();
		}

		public static Task<string> ReadFileAsTextAsync(BaseStorageFile file, int maxLength = 10 * 1024 * 1024)
			=> file.ReadTextAsync(maxLength);

		/// <summary>
		/// Call this function when you are ready to load the preview and details.
		/// Override if you need custom loading code.
		/// </summary>
		/// <returns>The task to run</returns>
		public virtual async Task LoadAsync()
		{
			List<FileProperty> detailsFull = [];

			if (Item.ItemFile is null)
			{
				var itemPath = Item.ItemPath!;
				var rootItem = await FilesystemTasks.WrapNullable(() => DriveHelpers.GetRootFromPathAsync(itemPath));
				Item.ItemFile = await StorageFileExtensions.DangerousGetFileFromPathAsync(itemPath, rootItem.Result);
			}

			await Task.Run(async () =>
			{
				DetailsFromPreview = await LoadPreviewAndDetailsAsync();
				if (userSettingsService.InfoPaneSettingsService.SelectedTab == InfoPaneTabs.Details)
				{
					// Add the details from the preview function, then the system file properties
					DetailsFromPreview?.ForEach(i => detailsFull.Add(i));
					List<FileProperty>? props = await GetSystemFilePropertiesAsync();
					if (props is not null)
						detailsFull.AddRange(props);
				}
			});

			Item.FileDetails = new System.Collections.ObjectModel.ObservableCollection<FileProperty>(detailsFull);
		}

		/// <summary>
		/// Override this and place the code to load the file preview here.
		/// You can return details that may have been obtained while loading the preview (eg. word count).
		/// This details will be displayed *before* the system file properties.
		/// If there are none, return an empty list.
		/// </summary>
		/// <returns>A list of details</returns>
		public async virtual Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var result = await FileThumbnailHelper.GetIconAsync(
				Item.ItemPath,
				Constants.ShellIconSizes.Jumbo,
				false,
				IconOptions.None);

			if (result is not null)
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () => FileImage = await result.ToBitmapAsync());
			else
				FileImage ??= await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => new BitmapImage());

			return [];
		}

		/// <summary>
		/// Override this if the preview control needs to handle the unloaded event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public virtual void PreviewControlBase_Unloaded(object? sender, RoutedEventArgs e)
			=> LoadCancelledTokenSource.Cancel();

		protected static FileProperty GetFileProperty(string nameResource, object? value)
			=> new() { NameResource = nameResource, Value = value };

		private async Task<List<FileProperty>?> GetSystemFilePropertiesAsync()
		{
			if (Item.IsShortcut)
				return null;
			if (Item.ItemFile is null)
				throw new InvalidOperationException("The preview item could not be opened as a file.");

			var list = await FileProperty.RetrieveAndInitializePropertiesAsync(Item.ItemFile,
				Constants.ResourceFilePaths.PreviewPaneDetailsPropertiesJsonPath);

			var address = list.Find(x => x.ID is "address")
				?? throw new InvalidDataException("The preview property definition is missing the address field.");
			var latitude = list.Find(x => x.Property is "System.GPS.LatitudeDecimal")
				?? throw new InvalidDataException("The preview property definition is missing the latitude field.");
			var longitude = list.Find(x => x.Property is "System.GPS.LongitudeDecimal")
				?? throw new InvalidDataException("The preview property definition is missing the longitude field.");
			address.Value = await LocationHelpers.GetAddressFromCoordinatesAsync(
				(double?)latitude.Value,
				(double?)longitude.Value);

			// Adds the value for the file tag
			var fileTag = list.FirstOrDefault(x => x.ID is "filetag")
				?? throw new InvalidDataException("The preview property definition is missing the file tag field.");
			fileTag.Value = Item.FileTagsUI is not null
				? string.Join(',', Item.FileTagsUI.Select(x => x.Name))
				: null;

			return list.Where(i => i.ValueText is not null).ToList();
		}

		private sealed partial class DetailsOnlyPreviewModel : BasePreviewModel
		{
			public DetailsOnlyPreviewModel(ListedItem item) : base(item) { }

			public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync() => Task.FromResult(DetailsFromPreview ?? []);
		}
	}
}
