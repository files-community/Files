// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class OpenReleaseNotesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IUpdateService UpdateService = Ioc.Default.GetRequiredService<IUpdateService>();

		public string Label
			=> Strings.ReleaseNotes.GetLocalizedResource();

		public string Description
			=> Strings.ReleaseNotesDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.AppUpdatedBox");

		public bool IsExecutable
			=> UpdateService.AreReleaseNotesAvailable;

		public OpenReleaseNotesAction()
		{
			UpdateService.PropertyChanged += UpdateService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return NavigationHelpers.OpenPathInNewTab("ReleaseNotes", true);
		}

		private void UpdateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IUpdateService.AreReleaseNotesAvailable):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
