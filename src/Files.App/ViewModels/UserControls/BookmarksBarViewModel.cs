$\xEF\xBB\xBF// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.UserControls
{
	public sealed class BookmarksBarViewModel : ObservableObject
	{
		private readonly IBookmarksService _bookmarksService = Ioc.Default.GetRequiredService<IBookmarksService>();
		private readonly IContentPageContext _contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		private Task? _loadTask;
		private bool _isEmpty = true;

		public ObservableCollection<BookmarkItem> Items { get; } = [];

		public bool IsEmpty
		{
			get => _isEmpty;
			private set => SetProperty(ref _isEmpty, value);
		}

		public BookmarksBarViewModel()
		{
			Items.CollectionChanged += (_, _) => IsEmpty = Items.Count == 0;
		}

		public Task InitializeAsync()
			=> _loadTask ??= LoadAsync();

		private async Task LoadAsync()
		{
			foreach (var item in await _bookmarksService.LoadAsync())
				Items.Add(item);
		}

		public async Task AddPathAsync(string? path)
		{
			// Make sure the saved bookmarks are loaded first so they are never overwritten.
			await InitializeAsync();

			if (string.IsNullOrWhiteSpace(path) ||
				Items.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)))
				return;

			var kind = GetKind(path);

			Items.Add(new BookmarkItem()
			{
				Title = GetTitle(path, kind),
				Path = path,
				Kind = kind
			});

			await _bookmarksService.SaveAsync(Items);
		}

		public async Task AddCurrentFolderAsync()
		{
			var path = _contentPageContext.ShellPage?.ShellViewModel?.WorkingDirectory;

			if (!string.IsNullOrEmpty(path) && SystemIO.Directory.Exists(path))
				await AddPathAsync(path);
		}

		public async Task RemoveAsync(BookmarkItem item)
		{
			if (Items.Remove(item))
				await _bookmarksService.SaveAsync(Items);
		}

		public async Task OpenAsync(BookmarkItem item)
		{
			var shellPage = _contentPageContext.ShellPage;

			if (shellPage is null || string.IsNullOrEmpty(item.Path))
				return;

			await NavigationHelpers.OpenPath(item.Path, shellPage, args: item.Arguments);
		}

		private static BookmarkKind GetKind(string path)
		{
			if (SystemIO.Directory.Exists(path))
				return BookmarkKind.Folder;

			var extension = SystemIO.Path.GetExtension(path).ToLowerInvariant();

			return extension is ".exe" or ".lnk" or ".bat" or ".cmd"
				? BookmarkKind.App
				: BookmarkKind.File;
		}

		private static string GetTitle(string path, BookmarkKind kind)
		{
			var trimmed = path.TrimEnd('\\', '/');

			var title = kind is BookmarkKind.File
				? SystemIO.Path.GetFileName(trimmed)
				: SystemIO.Path.GetFileNameWithoutExtension(trimmed);

			// Drive roots such as "C:\" have no file name
			return string.IsNullOrEmpty(title) ? path : title;
		}
	}
}
