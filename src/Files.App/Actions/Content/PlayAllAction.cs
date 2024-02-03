// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class PlayAllAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "PlayAll".GetLocalizedResource();

		public string Description
			=> "PlayAllDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE768");

		public bool IsExecutable =>
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.SelectedItems.Count > 1 &&
			ContentPageContext.SelectedItems.All(item => FileExtensionHelpers.IsMediaFile(item.FileExtension));

		public PlayAllAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return NavigationHelpers.OpenSelectedItemsAsync(ContentPageContext.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
