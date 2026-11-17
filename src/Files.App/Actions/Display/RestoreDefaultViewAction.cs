// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class RestoreDefaultViewAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.RestoreDefaultView.GetLocalizedResource();

		public string Description
			=> Strings.RestoreDefaultViewDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Layout;

		public bool IsExecutable
			=> context.ShellPage is not null;

		public RestoreDefaultViewAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			LayoutHelpers.ChangeFolderPreferences(context.ShellPage, LayoutPreferencesManager.ResetPreferencesForPath);
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
