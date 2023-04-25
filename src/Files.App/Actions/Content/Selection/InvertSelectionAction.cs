// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class InvertSelectionAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "InvertSelection".GetLocalizedResource();

		public string Description => "InvertSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE746");

		public bool IsExecutable
		{
			get
			{
				if (context.PageType is ContentPageTypes.Home)
					return false;

				if (!context.HasItem)
					return false;

				var page = context.ShellPage;
				if (page is null)
					return false;

				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return !isEditing && !isRenaming;
			}
		}

		public Task ExecuteAsync()
		{
			context?.ShellPage?.SlimContentPage?.ItemManipulationModel?.InvertSelection();
			return Task.CompletedTask;
		}
	}
}
