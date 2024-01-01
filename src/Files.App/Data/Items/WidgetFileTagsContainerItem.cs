// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagsContainerItem : ObservableObject, IAsyncInitialize
	{
		// Dependency injection

		private IFileTagsService FileTagsService { get; set; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		private ICommandManager Commands { get; set; } = Ioc.Default.GetRequiredService<ICommandManager>();
		private IImageService ImageService { get; set; } = Ioc.Default.GetRequiredService<IImageService>();

		// Fields

		private readonly string _tagUid;

		private readonly Func<string, Task> _openAction;

		// Properties

		public ObservableCollection<WidgetFileTagsItem> Tags { get; }

		// Events

		public delegate void SelectedTagChangedEventHandler(object sender, SelectedTagChangedEventArgs e);
		public static event SelectedTagChangedEventHandler? SelectedTagChanged;

		// Commands

		public ICommand ViewMoreCommand;
		public ICommand OpenAllCommand;

		private string? _Color;
		public string? Color
		{
			get => _Color;
			set => SetProperty(ref _Color, value);
		}

		private string? _Name;
		public string? Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		public WidgetFileTagsContainerItem(string tagUid, Func<string, Task> openAction)
		{
			_tagUid = tagUid;
			_openAction = openAction;
			Tags = new();

			ViewMoreCommand = new RelayCommand(ExecuteViewMoreCommand);
			OpenAllCommand = new RelayCommand(ExecuteOpenAllCommand);
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

		private void ExecuteViewMoreCommand()
		{
			_openAction($"tag:{Name}");

			return;
		}

		private void ExecuteOpenAllCommand()
		{
			SelectedTagChanged?.Invoke(this, new SelectedTagChangedEventArgs(Tags.Select(tag => (tag.Path, tag.IsFolder))));

			Commands.OpenAllTaggedItems.ExecuteAsync();

			return;
		}
	}
}
