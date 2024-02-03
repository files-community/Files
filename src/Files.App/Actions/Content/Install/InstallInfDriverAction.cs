// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class InstallInfDriverAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Install".GetLocalizedResource();
		
		public string Description
			=> "InstallInfDriverDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE9F5");

		public bool IsExecutable =>
			ContentPageContext.SelectedItems.Count == 1 &&
			FileExtensionHelpers.IsInfFile(ContentPageContext.SelectedItems[0].FileExtension) &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder;

		public InstallInfDriverAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (ListedItem selectedItem in ContentPageContext.SelectedItems)
				await Win32API.InstallInf(selectedItem.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
