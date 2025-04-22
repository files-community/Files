// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Text;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class DecompressArchiveDialogViewModel : ObservableObject
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private readonly IStorageFile archive;

		public BaseStorageFolder DestinationFolder { get; private set; }

		private string destinationFolderPath;
		public string DestinationFolderPath
		{
			get => destinationFolderPath;
			private set => SetProperty(ref destinationFolderPath, value);
		}

		private bool openDestinationFolderOnCompletion;
		public bool OpenDestinationFolderOnCompletion
		{
			get => openDestinationFolderOnCompletion;
			set => SetProperty(ref openDestinationFolderOnCompletion, value);
		}

		private bool isArchiveEncrypted;
		public bool IsArchiveEncrypted
		{
			get => isArchiveEncrypted;
			set => SetProperty(ref isArchiveEncrypted, value);
		}

		private bool isArchiveEncodingUndetermined;
		public bool IsArchiveEncodingUndetermined
		{
			get => isArchiveEncodingUndetermined;
			set => SetProperty(ref isArchiveEncodingUndetermined, value);
		}

		private Encoding? detectedEncoding;
		public Encoding? DetectedEncoding
		{
			get => detectedEncoding;
			set { 
				SetProperty(ref detectedEncoding, value);
				RefreshEncodingOptions();
			}
		}

		private bool showPathSelection;
		public bool ShowPathSelection
		{
			get => showPathSelection;
			set => SetProperty(ref showPathSelection, value);
		}

		public DisposableArray? Password { get; private set; }

		public EncodingItem[] EncodingOptions { get; set; } = EncodingItem.Defaults;
		public EncodingItem SelectedEncoding { get; set; }
		void RefreshEncodingOptions()
		{
			if (detectedEncoding != null)
			{
				EncodingOptions = EncodingItem.Defaults
				.Prepend(new EncodingItem(
					detectedEncoding, 
					string.Format(Strings.EncodingDetected.GetLocalizedResource(), detectedEncoding.EncodingName)
				))
				.ToArray();
			}
			else
			{
				EncodingOptions = EncodingItem.Defaults;
			}
			SelectedEncoding = EncodingOptions.FirstOrDefault();
		}

		

		public IRelayCommand PrimaryButtonClickCommand { get; private set; }

		public ICommand SelectDestinationCommand { get; private set; }

		public DecompressArchiveDialogViewModel(IStorageFile archive)
		{
			this.archive = archive;
			destinationFolderPath = DefaultDestinationFolderPath();
			SelectedEncoding = EncodingOptions[0];

			// Create commands
			SelectDestinationCommand = new AsyncRelayCommand(SelectDestinationAsync);
			PrimaryButtonClickCommand = new RelayCommand<DisposableArray>(password => Password = password);
		}

		private async Task SelectDestinationAsync()
		{
			bool result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var filePath);
			if (!result)
				return;

			DestinationFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(filePath);
			DestinationFolderPath = (DestinationFolder is not null) ? DestinationFolder.Path : DefaultDestinationFolderPath();
		}

		private string DefaultDestinationFolderPath()
		{
			return Path.Combine(Path.GetDirectoryName(archive.Path), Path.GetFileNameWithoutExtension(archive.Path));
		}
	}
}
