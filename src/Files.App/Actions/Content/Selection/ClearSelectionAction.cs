// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ClearSelectionAction : IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "ClearSelection".GetLocalizedResource();

		public string Description
			=> "ClearSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE8E6");

		public bool IsExecutable
		{
			get
			{
				if (ContentPageContext.PageType is ContentPageTypes.Home)
					return false;

				if (!ContentPageContext.HasSelection)
					return false;

				var page = ContentPageContext.ShellPage;
				if (page is null)
					return false;

				bool isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return isCommandPaletteOpen || (!isEditing && !isRenaming);
			}
		}

		public ClearSelectionAction()
		{
		}

		public Task ExecuteAsync()
		{
			ContentPageContext.ShellPage?.SlimContentPage?.ItemManipulationModel?.ClearSelection();

			return Task.CompletedTask;
		}
	}
}
