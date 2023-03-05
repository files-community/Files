using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Shared.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Files.Sdk.Storage.LocatableStorage;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public sealed partial class FileTagsContainerViewModel : ObservableObject, IAsyncInitialize
	{
		private readonly string _tagUid;
		private readonly Func<string, Task> _openAction;

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		private IImageService ImageService { get; } = Ioc.Default.GetRequiredService<IImageService>();

		public ObservableCollection<FileTagsItemViewModel> Tags { get; }

		[ObservableProperty]
		private string _Color;

		[ObservableProperty]
		private string _Name;

		public FileTagsContainerViewModel(string tagUid, Func<string, Task> openAction)
		{
			_tagUid = tagUid;
			_openAction = openAction;
			Tags = new();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetItemsForTagAsync(_tagUid, cancellationToken))
			{
				var icon = await ImageService.GetIconAsync(item.Storable, cancellationToken);
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
