// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
    sealed class OpenAllTaggedActions: ObservableObject, IAction
    {
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private ITagsContext TagsContext { get; } = Ioc.Default.GetRequiredService<ITagsContext>();

		public string Label
			=> "OpenAllTaggedItems".GetLocalizedResource();

		public string Description
			=> "OpenAllTaggedItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE71D");

		public bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			TagsContext.TaggedItems.Any();

		public OpenAllTaggedActions()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
			TagsContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var files = TagsContext.TaggedItems.Where(item => !item.isFolder);
			var folders = TagsContext.TaggedItems.Where(item => item.isFolder);

			await Task.WhenAll(files.Select(file 
				=> NavigationHelpers.OpenPath(file.path, ContentPageContext.ShellPage!)));

			folders.ForEach(async folder => await NavigationHelpers.OpenPathInNewTab(folder.path));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(ITagsContext.TaggedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
