// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Immutable;
using Windows.Foundation.Metadata;

namespace Files.App.Dialogs
{
	public sealed partial class CreateArchiveDialog : ContentDialog
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		private bool canCreate = false;
		public bool CanCreate => canCreate;

		public string FileName
		{
			get => ViewModel.FileName;
			set => ViewModel.FileName = value;
		}

		public bool UseEncryption
		{
			get => ViewModel.UseEncryption;
			set => ViewModel.UseEncryption = value;
		}

		public string Password
		{
			get => ViewModel.Password;
			set => ViewModel.Password = value;
		}

		public ArchiveFormats FileFormat
		{
			get => ViewModel.FileFormat.Key;
			set => ViewModel.FileFormat = ViewModel.FileFormats.First(format => format.Key == value);
		}

		public ArchiveCompressionLevels CompressionLevel
		{
			get => ViewModel.CompressionLevel.Key;
			set => ViewModel.CompressionLevel = ViewModel.CompressionLevels.First(level => level.Key == value);
		}

		public ArchiveSplittingSizes SplittingSize
		{
			get => ViewModel.SplittingSize.Key;
			set => ViewModel.SplittingSize = ViewModel.SplittingSizes.First(size => size.Key == value);
		}

		public ArchiveDictionarySizes DictionarySize
		{
			get => ViewModel.DictionarySize.Key;
			set => ViewModel.DictionarySize = ViewModel.DictionarySizes.First(size => size.Key == value);
		}

		public ArchiveWordSizes WordSize
		{
			get => ViewModel.WordSize.Key;
			set => ViewModel.WordSize = ViewModel.WordSizes.First(size => size.Key == value);
		}

		public int CPUThreads
		{
			get => ViewModel.CPUThreads;
			set => ViewModel.CPUThreads = value;
		}

		private DialogViewModel ViewModel { get; } = new();

		public CreateArchiveDialog()
		{
			InitializeComponent();

			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public new Task<ContentDialogResult> ShowAsync()
		{
			return SetContentDialogRoot(this).ShowAsync().AsTask();
		}

		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot; // WinUi3
			return contentDialog;
		}

		private void ContentDialog_Loaded(object _, RoutedEventArgs e)
		{
			Loaded -= ContentDialog_Loaded;

			FileNameBox.SelectionStart = FileNameBox.Text.Length;
			FileNameBox.Focus(FocusState.Programmatic);
		}
		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			InvalidNameWarning.IsOpen = false;
			Closing -= ContentDialog_Closing;
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

			if (e.Result is ContentDialogResult.Primary)
				canCreate = true;
		}

		private void PasswordBox_Loading(FrameworkElement _, object e)
			=> PasswordBox.Focus(FocusState.Programmatic);

		private void ViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(DialogViewModel.UseEncryption) && ViewModel.UseEncryption)
				PasswordBox.Focus(FocusState.Programmatic);
		}

		private sealed partial class DialogViewModel : ObservableObject
		{
			private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

			private readonly long _totalMemory;
			private readonly long _availableMemory;

			public bool IsNameValid => FilesystemHelpers.IsValidForFilename(fileName);

			public bool ShowNameWarning => !string.IsNullOrEmpty(fileName) && !IsNameValid;

			private string fileName = string.Empty;
			public string FileName
			{
				get => fileName;
				set
				{
					if (SetProperty(ref fileName, value))
					{
						OnPropertyChanged(nameof(IsNameValid));
						OnPropertyChanged(nameof(ShowNameWarning));
					}
				}
			}

			public FileFormatItem FileFormat
			{
				get => FileFormats.First(format => format.Key == GeneralSettingsService.ArchiveFormatsOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveFormatsOption)
					{
						GeneralSettingsService.ArchiveFormatsOption = value.Key;
						OnPropertyChanged(nameof(CanSplit));
						OnPropertyChanged(nameof(EstimatedMemoryText));
					}
				}
			}

			public CompressionLevelItem CompressionLevel
			{
				get => CompressionLevels.First(level => level.Key == GeneralSettingsService.ArchiveCompressionLevelsOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveCompressionLevelsOption)
					{
						GeneralSettingsService.ArchiveCompressionLevelsOption = value.Key;
						OnPropertyChanged(nameof(EstimatedMemoryText));
					}
				}
			}

			public bool CanSplit => FileFormat.Key is ArchiveFormats.SevenZip;

			public SplittingSizeItem SplittingSize
			{
				get => SplittingSizes.First(size => size.Key == GeneralSettingsService.ArchiveSplittingSizesOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveSplittingSizesOption)
						GeneralSettingsService.ArchiveSplittingSizesOption = value.Key;
				}
			}

			public DictionarySizeItem DictionarySize
			{
				get => DictionarySizes.First(size => size.Key == GeneralSettingsService.ArchiveDictionarySizesOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveDictionarySizesOption)
					{
						GeneralSettingsService.ArchiveDictionarySizesOption = value.Key;
						OnPropertyChanged(nameof(EstimatedMemoryText));
					}
				}
			}

			public WordSizeItem WordSize
			{
				get => WordSizes.First(size => size.Key == GeneralSettingsService.ArchiveWordSizesOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveWordSizesOption)
					{
						GeneralSettingsService.ArchiveWordSizesOption = value.Key;
						OnPropertyChanged(nameof(EstimatedMemoryText));
					}
				}
			}

			private int cpuThreads = Environment.ProcessorCount;
			public int CPUThreads
			{
				get => cpuThreads;
				set
				{
					if (SetProperty(ref cpuThreads, value))
						OnPropertyChanged(nameof(EstimatedMemoryText));
				}
			}

			private bool useEncryption = false;
			public bool UseEncryption
			{
				get => useEncryption;
				set
				{
					if (SetProperty(ref useEncryption, value) && !useEncryption)
						Password = string.Empty;
				}
			}

			private string password = string.Empty;
			public string Password
			{
				get => password;
				set
				{
					if (SetProperty(ref password, value) && !string.IsNullOrEmpty(password))
						UseEncryption = true;
				}
			}

			public ImmutableList<FileFormatItem> FileFormats { get; } =
			[
				new(ArchiveFormats.Zip, ".zip"),
				new(ArchiveFormats.SevenZip, ".7z"),
			];

			public ImmutableList<CompressionLevelItem> CompressionLevels { get; } =
			[
				new CompressionLevelItem(ArchiveCompressionLevels.Ultra, Strings.CompressionLevelUltra.GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.High, Strings.CompressionLevelHigh.GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Normal, Strings.CompressionLevelNormal.GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Low, Strings.CompressionLevelLow.GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Fast, Strings.CompressionLevelFast.GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.None, Strings.CompressionLevelNone.GetLocalizedResource()),
			];

			public ImmutableList<SplittingSizeItem> SplittingSizes { get; } =
			[
				new(ArchiveSplittingSizes.None, Strings.DoNotSplit.GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo10, ToSizeText(10)),
				new(ArchiveSplittingSizes.Mo100, ToSizeText(100)),
				new(ArchiveSplittingSizes.Cd650, ToSizeText(650), Strings.CD.GetLocalizedResource()),
				new(ArchiveSplittingSizes.Cd700, ToSizeText(700), Strings.CD.GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo1024, ToSizeText(1024)),
				new(ArchiveSplittingSizes.Mo2048, ToSizeText(2048)),
				new(ArchiveSplittingSizes.Fat4092, ToSizeText(4092), Strings.FAT.GetLocalizedResource()),
				new(ArchiveSplittingSizes.Dvd4480, ToSizeText(4480), Strings.DVD.GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo5120, ToSizeText(5120)),
				new(ArchiveSplittingSizes.Dvd8128, ToSizeText(8128), Strings.DVD.GetLocalizedResource()),
				new(ArchiveSplittingSizes.Bd23040, ToSizeText(23040), Strings.Bluray.GetLocalizedResource()),
			];

			public ImmutableList<DictionarySizeItem> DictionarySizes { get; } =
			[
				new(ArchiveDictionarySizes.Auto, Strings.Auto.GetLocalizedResource()),
				new(ArchiveDictionarySizes.Kb64, "64 KB"),
				new(ArchiveDictionarySizes.Kb256, "256 KB"),
				new(ArchiveDictionarySizes.Mb1, "1 MB"),
				new(ArchiveDictionarySizes.Mb2, "2 MB"),
				new(ArchiveDictionarySizes.Mb4, "4 MB"),
				new(ArchiveDictionarySizes.Mb8, "8 MB"),
				new(ArchiveDictionarySizes.Mb16, "16 MB"),
				new(ArchiveDictionarySizes.Mb32, "32 MB"),
				new(ArchiveDictionarySizes.Mb64, "64 MB"),
				new(ArchiveDictionarySizes.Mb128, "128 MB"),
				new(ArchiveDictionarySizes.Mb256, "256 MB"),
				new(ArchiveDictionarySizes.Mb512, "512 MB"),
				new(ArchiveDictionarySizes.Mb1024, "1024 MB"),
			];

			public ImmutableList<WordSizeItem> WordSizes { get; } =
			[
				new(ArchiveWordSizes.Auto, Strings.Auto.GetLocalizedResource()),
				new(ArchiveWordSizes.Fb8, "8"),
				new(ArchiveWordSizes.Fb16, "16"),
				new(ArchiveWordSizes.Fb32, "32"),
				new(ArchiveWordSizes.Fb64, "64"),
				new(ArchiveWordSizes.Fb128, "128"),
				new(ArchiveWordSizes.Fb256, "256"),
				new(ArchiveWordSizes.Fb273, "273"),
			];

			public DialogViewModel()
			{
				var gcMemoryInfo = GC.GetGCMemoryInfo();
				_totalMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
				_availableMemory = _totalMemory - gcMemoryInfo.MemoryLoadBytes;
			}

			public string EstimatedMemoryText
			{
				get
				{
					long estimated = EstimateCompressionMemory();
					return string.Format(
						Strings.EstimatedMemoryUsage.GetLocalizedResource(),
						((double)estimated).ToSizeString());
				}
			}

			public string AvailableMemoryText
			{
				get
				{
					return string.Format(
						Strings.AvailableMemory.GetLocalizedResource(),
						((double)_availableMemory).ToSizeString());
				}
			}

			private long EstimateCompressionMemory()
			{
				long dictBytes = GetEffectiveDictionaryBytes();
				int threads = Math.Max(1, cpuThreads);

				// LZMA2 compression memory estimation:
				// Per encoder instance: ~dictSize * 11.5 + 6 MB
				// LZMA2 uses ceil(threads / 2) encoder instances
				int numEncoders = (threads + 1) / 2;
				long perEncoder = (long)(dictBytes * 11.5) + 6 * 1024 * 1024;
				return perEncoder * numEncoders;
			}

			private long GetEffectiveDictionaryBytes()
			{
				var dictSizeKey = DictionarySize.Key;

				if (dictSizeKey is ArchiveDictionarySizes.Auto)
				{
					// Default dictionary sizes for LZMA2 based on compression level
					return CompressionLevel.Key switch
					{
						ArchiveCompressionLevels.Ultra => 64L * 1024 * 1024,
						ArchiveCompressionLevels.High => 32L * 1024 * 1024,
						ArchiveCompressionLevels.Normal => 16L * 1024 * 1024,
						ArchiveCompressionLevels.Low => 1L * 1024 * 1024,
						ArchiveCompressionLevels.Fast => 64L * 1024,
						_ => 0L
					};
				}

				return dictSizeKey switch
				{
					ArchiveDictionarySizes.Kb64 => 64L * 1024,
					ArchiveDictionarySizes.Kb256 => 256L * 1024,
					ArchiveDictionarySizes.Mb1 => 1L * 1024 * 1024,
					ArchiveDictionarySizes.Mb2 => 2L * 1024 * 1024,
					ArchiveDictionarySizes.Mb4 => 4L * 1024 * 1024,
					ArchiveDictionarySizes.Mb8 => 8L * 1024 * 1024,
					ArchiveDictionarySizes.Mb16 => 16L * 1024 * 1024,
					ArchiveDictionarySizes.Mb32 => 32L * 1024 * 1024,
					ArchiveDictionarySizes.Mb64 => 64L * 1024 * 1024,
					ArchiveDictionarySizes.Mb128 => 128L * 1024 * 1024,
					ArchiveDictionarySizes.Mb256 => 256L * 1024 * 1024,
					ArchiveDictionarySizes.Mb512 => 512L * 1024 * 1024,
					ArchiveDictionarySizes.Mb1024 => 1024L * 1024 * 1024,
					_ => 16L * 1024 * 1024
				};
			}

			private static string ToSizeText(ulong megaBytes) => ByteSize.FromMebiBytes(megaBytes).ShortString;

			public record FileFormatItem(ArchiveFormats Key, string Label);

			public record CompressionLevelItem(ArchiveCompressionLevels Key, string Label);

			public record DictionarySizeItem(ArchiveDictionarySizes Key, string Label);

			public record WordSizeItem(ArchiveWordSizes Key, string Label);
		}
	}

	internal record SplittingSizeItem(ArchiveSplittingSizes Key, string Label, string Description = "")
	{
		public string Separator => string.IsNullOrEmpty(Description) ? string.Empty : "-";
	}
}
