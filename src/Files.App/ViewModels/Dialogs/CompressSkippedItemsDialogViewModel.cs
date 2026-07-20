// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.Shared.Utils;

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class CompressSkippedItemsDialogViewModel : ObservableObject
	{
		public ObservableCollection<CompressSkippedItemViewModel> Items { get; }

		public string Description
			=> Strings.CompressSkippedItemsDialogText.GetLocalizedFormatResource(Items.Count);

		public CompressSkippedItemsDialogViewModel(IEnumerable<string> paths)
		{
			Items = new(paths.Select(path => new CompressSkippedItemViewModel(path)));

			_ = LoadItemsIconAsync();
		}

		private async Task LoadItemsIconAsync()
		{
			var imageService = Ioc.Default.GetRequiredService<IImageService>();

			foreach (var item in Items)
			{
				try
				{
					item.ItemIcon = await imageService.GetImageModelFromPathAsync(item.SourcePath, 64u);
				}
				catch (Exception)
				{
					// Icons are best-effort, e.g. the item may be inaccessible
				}
			}
		}
	}

	public sealed partial class CompressSkippedItemViewModel : ObservableObject
	{
		public string SourcePath { get; }

		public string DisplayName { get; }

		private IImage? _ItemIcon;
		public IImage? ItemIcon
		{
			get => _ItemIcon;
			set => SetProperty(ref _ItemIcon, value);
		}

		public CompressSkippedItemViewModel(string sourcePath)
		{
			SourcePath = sourcePath;
			DisplayName = SystemIO.Path.GetFileName(SystemIO.Path.TrimEndingDirectorySeparator(sourcePath));
		}
	}
}
