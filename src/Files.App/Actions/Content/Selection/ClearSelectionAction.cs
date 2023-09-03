// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ClearSelectionAction : IAction
	{
		private readonly IContentPageContext context;

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
				var page = context.ShellPage;
				bool isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
				if (isCommandPaletteOpen && context.HasSelection)
					return true;
				if (context.PageType is ContentPageTypes.Home)
					return false;

				if (!context.HasSelection)
					return false;

				
				if (page is null)
					return false;

				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return !isEditing && !isRenaming;
			}
		}

		public ClearSelectionAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.ClearSelection();

			return Task.CompletedTask;
		}
	}
}
