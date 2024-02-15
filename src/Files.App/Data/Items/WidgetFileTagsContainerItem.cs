// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagsContainerItem : ObservableObject
	{
		// Dependency injections

		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		private IImageService ImageService { get; } = Ioc.Default.GetRequiredService<IImageService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Properties

		public ObservableCollection<WidgetFileTagCardItem> Tags { get; } = [];

		public string TagId { get; set; } = string.Empty;

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

		// Events

		public delegate void SelectedTagChangedEventHandler(object sender, SelectedTagChangedEventArgs e);
		public static event SelectedTagChangedEventHandler? SelectedTagChanged;

		// Commands

		public ICommand ViewMoreCommand { get; }
		public ICommand OpenAllCommand { get; }

		public WidgetFileTagsContainerItem()
		{
			ViewMoreCommand = new AsyncRelayCommand(ExecuteViewMoreCommand);
			OpenAllCommand = new AsyncRelayCommand(ExecuteOpenAllCommand);
		}

		/// <inheritdoc/>
		public async Task InitializeAsync()
		{
			await foreach (var item in FileTagsService.GetItemsForTagAsync(TagId))
			{
				var icon = await ImageService.GetIconAsync(item.Storable);
				Tags.Add(new(item.Storable, icon));
			}
		}

		private Task ExecuteViewMoreCommand()
		{
			return NavigationHelpers.OpenPath($"tag:{Name}", ContentPageContext.ShellPage!);
		}

		private Task ExecuteOpenAllCommand()
		{
			SelectedTagChanged?.Invoke(this, new(Tags.Select(tag => (tag.Path, tag.IsFolder))));

			return Commands.OpenAllTaggedItems.ExecuteAsync();
		}
	}
}
