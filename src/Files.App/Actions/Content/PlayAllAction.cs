// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class PlayAllAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "PlayAll".GetLocalizedResource();

		public string Description
			=> "PlayAllDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE768");

		public bool IsExecutable =>
			context.PageType != ContentPageTypes.RecycleBin &&
			context.SelectedItems.Count > 1 &&
			context.SelectedItems.All(item => FileExtensionHelpers.IsMediaFile(item.FileExtension));

		public PlayAllAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return NavigationHelpers.OpenSelectedItemsAsync(context.ShellPage!);
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
