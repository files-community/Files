$\xEF\xBB\xBF// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class AddBookmarkAction : IAction
	{
		public string Label
			=> Strings.AddBookmark.GetLocalizedResource();

		public string Description
			=> Strings.AddBookmarkDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Navigation;

		public HotKey HotKey
			=> new(Keys.D, KeyModifiers.CtrlShift);

		public Task ExecuteAsync(object? parameter = null)
		{
			// Resolved lazily because view models are registered after actions are constructed
			return Ioc.Default.GetRequiredService<BookmarksBarViewModel>().AddCurrentFolderAsync();
		}
	}
}
