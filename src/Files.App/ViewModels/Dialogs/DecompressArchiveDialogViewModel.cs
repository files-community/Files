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
		// Services
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields
		private readonly IStorageFile archive;

		// Properties
		public BaseStorageFolder DestinationFolder { get; private set; }

		private string destinationFolderPath;
		public string DestinationFolderPath
		{
			get => destinationFolderPath;
			set
			{
				if (SetProperty(ref destinationFolderPath, value))
				{
					OnPropertyChanged(nameof(IsDestinationPathValid));
				}
			}
		}

		public bool IsDestinationPathValid
		{
			get
			{
				try
				{
					if (string.IsNullOrWhiteSpace(DestinationFolderPath))
						return false;

					string parentDir = Path.GetDirectoryName(DestinationFolderPath);
					string finalSegment = Path.GetFileName(DestinationFolderPath);

					// Check parent directory exists
					if (string.IsNullOrWhiteSpace(parentDir) || !Directory.Exists(parentDir))
						return false;

					// Check for invalid characters (IsValidForFilename already does this)
					if (!FilesystemHelpers.IsValidForFilename(finalSegment))
						return false;

					return true;
				}
				catch
				{
					// Catch any exception to prevent crashes
					return false;
				}
			}
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
			set
			{
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

		public ObservableCollection<string> PreviousExtractionLocations { get; } = [];

		// Commands
		public IRelayCommand PrimaryButtonClickCommand { get; private set; }
		public ICommand SelectDestinationCommand { get; private set; }
		public ICommand QuerySubmittedCommand { get; private set; }

		// Constructor
		public DecompressArchiveDialogViewModel(IStorageFile archive)
		{
			this.archive = archive;
			destinationFolderPath = DefaultDestinationFolderPath();
			SelectedEncoding = EncodingOptions[0];

			// Create commands
			SelectDestinationCommand = new AsyncRelayCommand(SelectDestinationAsync);
			PrimaryButtonClickCommand = new RelayCommand<DisposableArray>(password => Password = password);
		}

		// Private Methods
		private string DefaultDestinationFolderPath()
		{
			return Path.Combine(Path.GetDirectoryName(archive.Path), Path.GetFileNameWithoutExtension(archive.Path));
		}

		private async Task SelectDestinationAsync()
		{
			bool result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var filePath);
			if (!result)
				return;

			DestinationFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(filePath);
			DestinationFolderPath = (DestinationFolder is not null) ? DestinationFolder.Path : DefaultDestinationFolderPath();
		}

		private void RefreshEncodingOptions()
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

		// Public Methods
		public void UpdateSuggestions(string query)
		{
			var allItems = UserSettingsService.GeneralSettingsService.PreviousArchiveExtractionLocations;
			if (allItems is null)
				return;

			var filtered = allItems
				.Where(item => item.StartsWith(query, StringComparison.OrdinalIgnoreCase))
				.ToList();

			// Only update if results changed to prevent flickering
			if (!filtered.SequenceEqual(PreviousExtractionLocations))
			{
				PreviousExtractionLocations.Clear();
				foreach (var item in filtered)
					PreviousExtractionLocations.Add(item);
			}
		}
	}
}