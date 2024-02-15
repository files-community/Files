// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public sealed class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Dependency injections

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = [];

		public string WidgetName => nameof(FileTagsWidgetViewModel);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Events

		public static event EventHandler<IEnumerable<WidgetFileTagCardItem>>? SelectedTaggedItemsChanged;

		// Constructor

		public FileTagsWidgetViewModel()
		{
			_ = InitializeWidget();
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public async Task InitializeWidget()
		{
			Containers.Clear();

			await foreach (var item in FileTagsService.GetTagsAsync())
			{
				var container = new WidgetFileTagsContainerItem()
				{
					TagId = item.Uid,
					Name = item.Name,
					Color = item.Color
				};

				Containers.Add(container);

				_ = container.InitializeAsync();
			}
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}
