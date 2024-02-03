// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal abstract class BaseSetAsAction : ObservableObject, IAction
	{
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public virtual bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
			(ContentPageContext.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public BaseSetAsAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync();

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IContentPageContext.SelectedItem):
				case nameof(IContentPageContext.SelectedItems):
					{
						if (ContentPageContext.ShellPage is not null && ContentPageContext.ShellPage.SlimContentPage is not null)
						{
							var viewModel = ContentPageContext.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
							var extensions = ContentPageContext.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

							viewModel.CheckAllFileExtensions(extensions);
						}

						OnPropertyChanged(nameof(IsExecutable));
						break;
					}
			}
		}
	}
}
