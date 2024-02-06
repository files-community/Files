// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class InstallInfDriverAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Install".GetLocalizedResource();
		
		public string Description
			=> "InstallInfDriverDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE9F5");

		public bool IsExecutable =>
			context.SelectedItems.Count == 1 &&
			FileExtensionHelpers.IsInfFile(context.SelectedItems[0].FileExtension) &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder;

		public InstallInfDriverAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await Task.WhenAll(context.SelectedItems.Select(selectedItem => Win32API.InstallInf(selectedItem.ItemPath)));
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
