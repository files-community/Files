// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RenameAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Rename".GetLocalizedResource();

		public string Description
			=> "RenameDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F2);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRename");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			IsPageTypeValid() &&
			context.ShellPage.SlimContentPage is not null &&
			IsSelectionValid();

		public RenameAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel.StartRenameItem();

			return Task.CompletedTask;
		}

		private bool IsSelectionValid()
		{
			return context.HasSelection && context.SelectedItems.Count == 1;
		}

		private bool IsPageTypeValid()
		{
			return
				context.PageType != ContentPageTypes.None &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder;
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
