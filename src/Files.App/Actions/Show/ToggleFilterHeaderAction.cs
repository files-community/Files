// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleFilterHeaderAction : ObservableObject, IToggleAction
	{
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.ToggleFilterHeader.GetLocalizedResource();

		public string Description
			=> Strings.ToggleFilterHeaderDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Filter");

		public HotKey HotKey
			=> new(Keys.F, KeyModifiers.CtrlShift);

		public bool IsOn
			=> generalSettingsService.ShowFilterHeader;

		public ToggleFilterHeaderAction()
		{
			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			generalSettingsService.ShowFilterHeader = !IsOn;

			if (IsOn)
				ContentPageContext.ShellPage!.ShellViewModel.InvokeFocusFilterHeader();
			else
				ContentPageContext.ShellPage!.PaneHolder.FocusActivePane();

				return Task.CompletedTask;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(GeneralSettingsService.ShowFilterHeader))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
