// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

namespace Files.App.Actions
{
	internal class OpenDirectoryInNewPaneAction : ObservableObject, IExtendedAction
	{
		private readonly IContentPageContext context;

		private readonly IUserSettingsService userSettingsService;

		public string Label
			=> "OpenInNewPane".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewPaneDescription".GetLocalizedResource();

		public bool IsExecutable =>
			((context.SelectedItem is not null &&
			context.SelectedItem.IsFolder) ||
			Parameter is not null) &&
			userSettingsService.GeneralSettingsService.ShowOpenInNewPane;

		public object? Parameter { get; set; }

		public OpenDirectoryInNewPaneAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (Parameter is not null && Parameter is WidgetCardItem item)
			{
				context.ShellPage.PaneHolder?.OpenPathInNewPane(item.Path);

				return Task.CompletedTask;
			}

			NavigationHelpers.OpenInSecondaryPane(
				context.ShellPage,
				context.ShellPage.SlimContentPage.SelectedItems.FirstOrDefault());

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
