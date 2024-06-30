// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class ArrangePanesVerticallyAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> "ArrangePanesVertically".GetLocalizedResource();

		public string Description
			=> "ArrangePanesVerticallyDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconAddVerticalPane");

		public bool IsOn
			=> GeneralSettingsService.ShellPaneArrangement == ShellPaneArrangement.Vertical;

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneEnabled &&
			ContentPageContext.IsMultiPaneActive;

		public ArrangePanesVerticallyAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			// Change direction

			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsMultiPaneEnabled):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
