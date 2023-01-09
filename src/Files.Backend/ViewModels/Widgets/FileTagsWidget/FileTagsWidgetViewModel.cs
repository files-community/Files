using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Shared.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
	public sealed partial class FileTagsWidgetViewModel : ObservableObject, IAsyncInitialize
	{
		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		public ObservableCollection<FileTagsContainerViewModel> Containers { get; }

		public FileTagsWidgetViewModel()
		{
			Containers = new();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
			{
				Containers.Add(new()
				{
					TagName = item,
					TagColor = null // TODO: Add tag color
				});
			}
		}
	}
}
