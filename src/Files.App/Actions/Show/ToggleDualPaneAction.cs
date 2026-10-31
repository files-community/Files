// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleDualPaneAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.ToggleDualPane.GetLocalizedResource();

		public string Description
			=> Strings.ToggleDualPaneDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.DualPane;

		public HotKey HotKey
		=> new(Keys.S, KeyModifiers.CtrlShift);

		public bool IsOn
			=> ContentPageContext.IsMultiPaneActive;

		public ToggleDualPaneAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (IsOn)
			{
				if (ContentPageContext.ShellPage is { } shellPage)
					shellPage.GetRequiredPaneHolder().CloseOtherPane();
			}
			else
			{
				if (ContentPageContext.ShellPage is not { } shellPage)
					return Task.CompletedTask;

				var paneHolder = shellPage.GetRequiredPaneHolder();
				var shellViewModel = shellPage.GetRequiredShellViewModel();
				paneHolder.OpenSecondaryPane(shellViewModel.WorkingDirectory ?? string.Empty, generalSettingsService.ShellPaneArrangementOption);
			}

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
