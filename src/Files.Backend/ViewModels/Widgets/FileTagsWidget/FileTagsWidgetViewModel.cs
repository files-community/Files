using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Shared.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Files.Sdk.Storage.LocatableStorage;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
	public sealed partial class FileTagsWidgetViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly Func<ILocatableStorable, Task> _openAction;

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		public ObservableCollection<FileTagsContainerViewModel> Containers { get; }

		public FileTagsWidgetViewModel(Func<ILocatableStorable, Task> openAction)
		{
			_openAction = openAction;
			Containers = new();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
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
