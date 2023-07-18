// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
    sealed class OpenAllTaggedActions: ObservableObject, IAction
    {
		private readonly IContentPageContext _pageContext;

		private readonly ITagsContext _tagsContext;

		public string Label
			=> "OpenAllItems".GetLocalizedResource();

		public string Description
			=> "OpenAllItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE71D");

		public bool IsExecutable => 
			_pageContext.ShellPage is not null &&
			_tagsContext.TaggedItems.Any();

		public OpenAllTaggedActions()
		{
			_pageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			_tagsContext = Ioc.Default.GetRequiredService<ITagsContext>();

			_pageContext.PropertyChanged += Context_PropertyChanged;
			_tagsContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var files = _tagsContext.TaggedItems.Where(item => !item.IsFolder);
			var folders = _tagsContext.TaggedItems.Where(item => item.IsFolder);

			await Task.WhenAll(files.Select(file 
				=> NavigationHelpers.OpenPath(file.Path, _pageContext.ShellPage!)));

			folders.ForEach(async folder => await NavigationHelpers.OpenPathInNewTab(folder.Path));
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
