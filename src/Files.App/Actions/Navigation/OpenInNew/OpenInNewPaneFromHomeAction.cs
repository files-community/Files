// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenInNewPaneFromHomeAction : BaseOpenInNewPaneAction
	{
		public override bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			ContentPageContext.SelectedItem.IsFolder &&
			UserSettingsService.GeneralSettingsService.ShowOpenInNewPane;

		public override Task ExecuteAsync()
		{
			NavigationHelpers.OpenInSecondaryPane(
				ContentPageContext.ShellPage,
				ContentPageContext.ShellPage.SlimContentPage.SelectedItems.FirstOrDefault());

			return Task.CompletedTask;
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
