// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenReleaseNotesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IUpdateService UpdateService = Ioc.Default.GetRequiredService<IUpdateService>();

		public string Label
			=> Strings.ReleaseNotes.GetLocalizedResource();

		public string Description
			=> Strings.ReleaseNotesDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

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
					// Raised from the background release-notes check; IsExecutable feeds
					// XAML-bound command state, so raise the change on the UI thread
					MainWindow.Instance.DispatcherQueue.TryEnqueue(() => OnPropertyChanged(nameof(IsExecutable)));
					break;
			}
		}
	}
}
