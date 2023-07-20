// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Shared.Utils;

namespace Files.App.ViewModels.Widgets
{
	public sealed partial class FileTagsContainerViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly string _tagUid;

		private readonly Func<string, Task> _openAction;

		private readonly IFileTagsService _fileTagsService;

		private readonly IImageService _imageService;

		private readonly ICommandManager _commands;

		public delegate void SelectedTagChangedEventHandler(object sender, SelectedTagChangedEventArgs e);

		public static event SelectedTagChangedEventHandler? SelectedTagChanged;

		public ObservableCollection<FileTagsItemViewModel> Tags { get; }

		[ObservableProperty]
		private string _Color;

		[ObservableProperty]
		private string _Name;

		public FileTagsContainerViewModel(string tagUid, Func<string, Task> openAction)
		{
			_fileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();
			_imageService = Ioc.Default.GetRequiredService<IImageService>();
			_commands = Ioc.Default.GetRequiredService<ICommandManager>();

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

		[RelayCommand]
		private Task OpenAll(CancellationToken cancellationToken)
		{
			SelectedTagChanged?.Invoke(this, new SelectedTagChangedEventArgs(Tags.Select(tag => (tag.Path, tag.IsFolder))));

			return _commands.OpenAllTaggedItems.ExecuteAsync();
		}
	}
}
