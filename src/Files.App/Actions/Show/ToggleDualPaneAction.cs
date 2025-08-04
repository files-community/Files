// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class ToggleDualPaneAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.ToggleDualPane.GetLocalizedResource();

		public string Description
			=> Strings.ToggleDualPaneDescription.GetLocalizedResource();

		public bool IsOn
			=> ContentPageContext.IsMultiPaneActive;

		public ToggleDualPaneAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (IsOn)
				ContentPageContext.ShellPage?.PaneHolder.CloseOtherPane();
			else
				ContentPageContext.ShellPage?.PaneHolder.OpenSecondaryPane(arrangement: generalSettingsService.ShellPaneArrangementOption);

			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}
}
