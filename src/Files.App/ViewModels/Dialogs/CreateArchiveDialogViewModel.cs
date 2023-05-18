// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Archive;
using Files.Backend.Models;
using System.Collections.Immutable;

namespace Files.App.ViewModels.Dialogs
{
	public class CreateArchiveDialogViewModel : ObservableObject
	{
		public bool IsNameValid
			=> FilesystemHelpers.IsValidForFilename(fileName);

		public bool ShowNameWarning
			=> !string.IsNullOrEmpty(fileName) && !IsNameValid;

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

		private FileFormatItem fileFormat;
		public FileFormatItem FileFormat
		{
			get => fileFormat;
			set
			{
				if (SetProperty(ref fileFormat, value))
					OnPropertyChanged(nameof(CanSplit));
			}
		}

		private CompressionLevelItem compressionLevel;
		public CompressionLevelItem CompressionLevel
		{
			get => compressionLevel;
			set => SetProperty(ref compressionLevel, value);
		}

		public bool CanSplit
			=> FileFormat.Key is ArchiveFormats.SevenZip;

		private SevenZipSplittingSizeItem splittingSize;
		public SevenZipSplittingSizeItem SplittingSize
		{
			get => splittingSize;
			set => SetProperty(ref splittingSize, value);
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

		public IImmutableList<FileFormatItem> FileFormats { get; } =
			new List<FileFormatItem>
			{
				new(ArchiveFormats.Zip, ".zip"),
				new(ArchiveFormats.SevenZip, ".7z"),
			}.ToImmutableList();

		public IImmutableList<CompressionLevelItem> CompressionLevels { get; } =
			new List<CompressionLevelItem>
			{
				new CompressionLevelItem(ArchiveCompressionLevels.Ultra, "CompressionLevelUltra".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.High, "CompressionLevelHigh".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Normal, "CompressionLevelNormal".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Low, "CompressionLevelLow".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Fast, "CompressionLevelFast".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.None, "CompressionLevelNone".GetLocalizedResource()),
			}.ToImmutableList();

		public IImmutableList<SevenZipSplittingSizeItem> SplittingSizes { get; } =
			new List<SevenZipSplittingSizeItem>
			{
				new(ArchiveSplittingSizes.None, "Do not split".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo10, ToSizeText(10)),
				new(ArchiveSplittingSizes.Mo100, ToSizeText(100)),
				new(ArchiveSplittingSizes.Cd650, ToSizeText(650), "CD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Cd700, ToSizeText(700), "CD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo1024, ToSizeText(1024)),
				new(ArchiveSplittingSizes.Fat4092, ToSizeText(4092), "FAT".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Dvd4480, ToSizeText(4480), "DVD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo5120, ToSizeText(5120)),
				new(ArchiveSplittingSizes.Dvd8128, ToSizeText(8128), "DVD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Bd23040, ToSizeText(23040), "Bluray".GetLocalizedResource()),
			}.ToImmutableList();

		public CreateArchiveDialogViewModel()
		{
			fileFormat = FileFormats.First(format => format.Key is ArchiveFormats.Zip);
			compressionLevel = CompressionLevels.First(level => level.Key is ArchiveCompressionLevels.Normal);
			splittingSize = SplittingSizes.First(size => size.Key is ArchiveSplittingSizes.None);
		}

		private static string ToSizeText(ulong megaBytes)
			=> ByteSize.FromMebiBytes(megaBytes).ShortString;

		public record FileFormatItem(ArchiveFormats Key, string Label);

		public record CompressionLevelItem(ArchiveCompressionLevels Key, string Label);
	}
}
