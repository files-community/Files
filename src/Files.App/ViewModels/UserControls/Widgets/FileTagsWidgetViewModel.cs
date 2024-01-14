// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Utils;

namespace Files.App.UserControls.ViewModels.Widgets
{
	public sealed partial class FileTagsWidgetViewModel : ObservableObject, IAsyncInitialize
	{
		// Dependency injections

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		// Fields

		private readonly Func<string, Task> _openAction;

		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; }

		// Constructor

		public FileTagsWidgetViewModel(Func<string, Task> openAction)
		{
			_openAction = openAction;
			Containers = new();
		}

		// Methods

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
			{
				var container = new WidgetFileTagsContainerItem(item.Uid, _openAction)
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
