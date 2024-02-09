// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagsContainerItem : ObservableObject, IAsyncInitialize
	{
		// Fields

		private readonly IFileTagsService FileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();
		private readonly IImageService ImageService = Ioc.Default.GetRequiredService<IImageService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly string _tagUid;

		private readonly Func<string, Task> _openAction;

		// Properties

		public ObservableCollection<WidgetFileTagCardItem> Tags { get; }

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

		public WidgetFileTagsContainerItem(string tagUid, Func<string, Task> openAction)
		{
			_tagUid = tagUid;
			_openAction = openAction;
			Tags = new();

			ViewMoreCommand = new AsyncRelayCommand<CancellationToken>(ViewMore);
			OpenAllCommand = new AsyncRelayCommand<CancellationToken>(OpenAll);
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

		private Task ViewMore(CancellationToken cancellationToken)
		{
			return _openAction($"tag:{Name}");
		}

		private Task OpenAll(CancellationToken cancellationToken)
		{
			SelectedTagChanged?.Invoke(this, new SelectedTagChangedEventArgs(Tags.Select(tag => (tag.Path, tag.IsFolder))));

			return Commands.OpenAllTaggedItems.ExecuteAsync();
		}
	}
}
