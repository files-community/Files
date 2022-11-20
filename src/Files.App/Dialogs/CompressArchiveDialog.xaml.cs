using Files.App.Extensions;
using Files.App.Filesystem.Archive;
using Files.Backend.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace Files.App.Dialogs
{
	public sealed partial class CompressArchiveDialog : ContentDialog
	{
		public static readonly DependencyProperty FileNameProperty = DependencyProperty
			.Register(nameof(FileName), typeof(string), typeof(CompressArchiveDialog), new(string.Empty));

		public static readonly DependencyProperty PasswordProperty = DependencyProperty
			.Register(nameof(Password), typeof(string), typeof(CompressArchiveDialog), new(string.Empty));

		public static readonly DependencyProperty FileFormatProperty = DependencyProperty
			.Register(nameof(FileFormat), typeof(ArchiveFormats), typeof(CompressArchiveDialog), new(ArchiveFormats.Zip));

		public static readonly DependencyProperty CompressionLevelProperty = DependencyProperty
			.Register(nameof(CompressionLevel), typeof(ArchiveCompressionLevels), typeof(CompressArchiveDialog), new(ArchiveCompressionLevels.Normal));

		public static readonly DependencyProperty SplittingSizeProperty = DependencyProperty
			.Register(nameof(SplittingSize), typeof(ArchiveSplittingSizes), typeof(CompressArchiveDialog), new(ArchiveSplittingSizes.None));

		private bool canCreate = false;
		public bool CanCreate => canCreate;

		public string FileName
		{
			get => (string)GetValue(FileNameProperty);
			set => SetValue(FileNameProperty, value);
		}

		public string Password
		{
			get => (string)GetValue(PasswordProperty);
			set => SetValue(PasswordProperty, value);
		}

		public ArchiveFormats FileFormat
		{
			get => (ArchiveFormats)GetValue(FileFormatProperty);
			set => SetValue(FileFormatProperty, (int)value);
		}
		public ArchiveCompressionLevels CompressionLevel
		{
			get => (ArchiveCompressionLevels)GetValue(CompressionLevelProperty);
			set => SetValue(CompressionLevelProperty, (int)value);
		}
		public ArchiveSplittingSizes SplittingSize
		{
			get => (ArchiveSplittingSizes)GetValue(SplittingSizeProperty);
			set => SetValue(SplittingSizeProperty, (int)value);
		}

		private IImmutableList<FileFormatItem> FileFormats { get; } = new List<FileFormatItem>
		{
			new(ArchiveFormats.Zip, ".zip", "CompressionFormatZipDescription".GetLocalizedResource()),
			new(ArchiveFormats.SevenZip, ".7z", "CompressionFormatSevenZipDescription".GetLocalizedResource()),
		}.ToImmutableList();

		private IImmutableList<CompressionLevelItem> CompressionLevels { get; } = new List<CompressionLevelItem>
		{
			new(ArchiveCompressionLevels.Ultra, "CompressionLevelUltra".GetLocalizedResource()),
			new(ArchiveCompressionLevels.High, "CompressionLevelHigh".GetLocalizedResource()),
			new(ArchiveCompressionLevels.Normal, "CompressionLevelNormal".GetLocalizedResource()),
			new(ArchiveCompressionLevels.Low, "CompressionLevelLow".GetLocalizedResource()),
			new(ArchiveCompressionLevels.Fast, "CompressionLevelFast".GetLocalizedResource()),
			new(ArchiveCompressionLevels.None, "CompressionLevelNone".GetLocalizedResource()),
		}.ToImmutableList();

		private IImmutableList<SplittingSizeItem> SplittingSizes { get; } = new List<SplittingSizeItem>
		{
			new(ArchiveSplittingSizes.None, "Do not split".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Mo10, ToSizeText(10)),
			new(ArchiveSplittingSizes.Mo100, ToSizeText(100)),
			new(ArchiveSplittingSizes.Cd650, ToSizeText(650, "CD".GetLocalizedResource())),
			new(ArchiveSplittingSizes.Cd700, ToSizeText(700, "CD".GetLocalizedResource())),
			new(ArchiveSplittingSizes.Mo1024, ToSizeText(1024)),
			new(ArchiveSplittingSizes.Fat4092, ToSizeText(4092, "FAT".GetLocalizedResource())),
			new(ArchiveSplittingSizes.Dvd4480, ToSizeText(4480, "DVD".GetLocalizedResource())),
			new(ArchiveSplittingSizes.Mo5120, ToSizeText(5120)),
			new(ArchiveSplittingSizes.Dvd8128, ToSizeText(8128, "DVD".GetLocalizedResource())),
			new(ArchiveSplittingSizes.Bd23040, ToSizeText(23040, "Bluray".GetLocalizedResource())),
		}.ToImmutableList();

		public CompressArchiveDialog() => InitializeComponent();

		public new Task<ContentDialogResult> ShowAsync() => SetContentDialogRoot(this).ShowAsync().AsTask();

		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = App.Window.Content.XamlRoot; // WinUi3
			return contentDialog;
		}

		private static string ToSizeText(ulong size) => ByteSize.FromMebiBytes(size).ShortString;
		private static string ToSizeText(ulong size, string labelKey) => $"{ToSizeText(size)} - {labelKey}";

		private void ContentDialog_Loaded(object _, RoutedEventArgs e)
		{
			Loaded -= ContentDialog_Loaded;

			FileFormatSelector.SelectedItem = FileFormats.First(format => format.Key == FileFormat);
			CompressionLevelSelector.SelectedItem = CompressionLevels.First(level => level.Key == CompressionLevel);
			SplittingSizeSelector.SelectedItem = SplittingSizes.First(size => size.Key == SplittingSize);

			UseEncryption.IsOn = Password.Length > 0;
			FileNameBox.SelectionStart = FileNameBox.Text.Length;
			FileNameBox.Focus(FocusState.Programmatic);
		}
		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			Closing -= ContentDialog_Closing;
			FileFormatSelector.SelectionChanged -= FileFormatSelector_SelectionChanged;

			if (e.Result is ContentDialogResult.Primary)
				canCreate = true;
		}

		private void FileFormatSelector_SelectionChanged(object _, SelectionChangedEventArgs e)
		{
			SplittingSizeSelector.IsEnabled = FileFormat is ArchiveFormats.SevenZip;
		}
		private void UseEncryption_Toggled(object _, RoutedEventArgs e)
		{
			if (UseEncryption.IsOn)
				PasswordBox.Focus(FocusState.Programmatic);
			else
				Password = string.Empty;
		}
		private void PasswordBox_PasswordChanging(PasswordBox _, PasswordBoxPasswordChangingEventArgs e)
		{
			if (PasswordBox.Password.Length > 0)
				UseEncryption.IsOn = true;
		}

		private record FileFormatItem(ArchiveFormats Key, string Label, string Description);
		private record CompressionLevelItem(ArchiveCompressionLevels Key, string Label);
		private record SplittingSizeItem(ArchiveSplittingSizes Key, string Label);
	}
}
