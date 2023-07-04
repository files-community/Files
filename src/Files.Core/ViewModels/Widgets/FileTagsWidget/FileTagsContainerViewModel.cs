// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Utils;

namespace Files.Core.ViewModels.Widgets.FileTagsWidget
{
	public sealed partial class FileTagsContainerViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly IFileTagsService _fileTagsService;

		private readonly IImageService _imageService;

		private readonly string _tagUid;

		private readonly Func<string, Task> _openAction;

		public ObservableCollection<FileTagsItemViewModel> Tags { get; }

		private string _Color;
		public string Color
		{
			get => _Color;
			set => SetProperty(ref _Color, value);
		}

		private string _Name;
		public string Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		public FileTagsContainerViewModel(string tagUid, Func<string, Task> openAction)
		{
			_fileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();
			_imageService = Ioc.Default.GetRequiredService<IImageService>();

			_tagUid = tagUid;
			_openAction = openAction;
			Tags = new();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in _fileTagsService.GetItemsForTagAsync(_tagUid, cancellationToken))
			{
				var icon = await _imageService.GetIconAsync(item.Storable, cancellationToken);

				Tags.Add(new(item.Storable, _openAction, icon));
			}
		}

		[RelayCommand]
		private Task ViewMore(CancellationToken cancellationToken)
		{
			return _openAction($"tag:{Name}");
		}
	}
}
