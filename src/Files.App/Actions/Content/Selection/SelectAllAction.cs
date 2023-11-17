// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SelectAllAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "SelectAll".GetLocalizedResource();

		public string Description
			=> "SelectAllDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE8B3");

		public HotKey HotKey
			=> new(Keys.A, KeyModifiers.Ctrl);

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

				bool isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage?.IsRenamingItem ?? false;

				return isCommandPaletteOpen || (!isEditing && !isRenaming);
			}
		}

		public SelectAllAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.SelectAllItems();

			return Task.CompletedTask;
		}
	}
}
