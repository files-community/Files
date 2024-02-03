// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class InstallFontAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Install".GetLocalizedResource();

		public string Description
			=> "InstallFontDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE8D2");

		public bool IsExecutable =>
			ContentPageContext.SelectedItems.Any() &&
			ContentPageContext.SelectedItems.All(x => FileExtensionHelpers.IsFontFile(x.FileExtension)) &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder;

		public InstallFontAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var paths = ContentPageContext.SelectedItems.Select(item => item.ItemPath).ToArray();
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
