// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class InstallFontAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Install".GetLocalizedResource();

		public string Description
			=> "InstallFontDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE8D2");

		public bool IsExecutable =>
			context.SelectedItems.Any() &&
			context.SelectedItems.All(x => FileExtensionHelpers.IsFontFile(x.FileExtension)) &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder;

		public InstallFontAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var paths = context.SelectedItems.Select(item => item.ItemPath).ToArray();
			return Win32API.InstallFontsAsync(paths, false);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
