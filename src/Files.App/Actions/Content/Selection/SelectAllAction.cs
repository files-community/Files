// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class SelectAllAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "SelectAll".GetLocalizedResource();

		public string Description => "SelectAllDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE8B3");

		public HotKey HotKey { get; } = new(Keys.A, KeyModifiers.Ctrl);

		public bool IsExecutable
		{
			get
			{
				if (context.PageType is ContentPageTypes.Home)
					return false;

				var page = context.ShellPage;
				if (page is null)
					return false;

				int itemCount = page.FilesystemViewModel.FilesAndFolders.Count;
				int selectedItemCount = context.SelectedItems.Count;
				if (itemCount == selectedItemCount)
					return false;

				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return !isEditing && !isRenaming;
			}
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.SelectAllItems();
			return Task.CompletedTask;
		}
	}
}
