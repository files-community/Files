// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Utils;

namespace Files.Core.ViewModels.Widgets.FileTagsWidget
{
	public sealed partial class FileTagsWidgetViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly Func<string, Task> _openAction;

		private readonly IFileTagsService _fileTagsService;

		private readonly IFileTagsSettingsService _fileTagsSettingsService;

		public ObservableCollection<FileTagsContainerViewModel> Containers { get; }

		public FileTagsWidgetViewModel(Func<string, Task> openAction)
		{
			// Dependency injection
			_fileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();
			_fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

			_openAction = openAction;
			Containers = new();

			_fileTagsSettingsService.OnTagsUpdated += FileTagsSettingsService_OnTagsUpdated;
		}

		private async void FileTagsSettingsService_OnTagsUpdated(object? _, EventArgs e)
		{
			Containers.Clear();

			await InitAsync();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in _fileTagsService.GetTagsAsync(cancellationToken))
			{
				var container = new FileTagsContainerViewModel(item.Uid, _openAction)
				{
					Name = item.Name,
					Color = item.Color
				};

				Containers.Add(container);

				_ = container.InitAsync(cancellationToken);
			}
		}
	}
}
