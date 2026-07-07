// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenCurrentFolderInOtherPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.OpenCurrentFolderInOtherPane.GetLocalizedResource();

		public string Description
			=> Strings.OpenCurrentFolderInOtherPaneDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.DualPane;

		public bool IsExecutable =>
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.IsMultiPaneActive &&
			!string.IsNullOrEmpty(ContentPageContext.ShellPage?.ShellViewModel?.WorkingDirectory);

		public OpenCurrentFolderInOtherPaneAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var path = ContentPageContext.ShellPage?.ShellViewModel?.WorkingDirectory;
			if (string.IsNullOrEmpty(path))
				return Task.CompletedTask;

			ContentPageContext.ShellPage?.PaneHolder?.OpenInOtherPane(path);

			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}