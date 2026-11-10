// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleTerminalPaneAction : ObservableObject, IToggleAction
	{
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public string Label
			=> Strings.ToggleTerminalPane.GetLocalizedResource();

		public string Description
			=> Strings.ToggleTerminalPaneDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public RichGlyph Glyph
			=> new("\uE756");

		public bool IsAccessibleGlobally
			=> AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev && generalSettingsService.IsTerminalIntegrationEnabled;

		public bool IsExecutable
			=> AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev && generalSettingsService.IsTerminalIntegrationEnabled;

		public bool IsOn
			=> mainPageViewModel.IsTerminalViewOpen;

		public ToggleTerminalPaneAction()
		{
			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
			mainPageViewModel.PropertyChanged += MainPageViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			mainPageViewModel.TerminalToggleCommand.Execute(null);
			return Task.CompletedTask;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(GeneralSettingsService.IsTerminalIntegrationEnabled))
			{
				OnPropertyChanged(nameof(IsExecutable));
				OnPropertyChanged(nameof(IsAccessibleGlobally));
			}
		}

		private void MainPageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(MainPageViewModel.IsTerminalViewOpen))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
