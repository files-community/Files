// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal abstract class BaseRotateAction : ObservableObject, IAction
	{
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected InfoPaneViewModel InfoPaneViewModel { get; } = Ioc.Default.GetRequiredService<InfoPaneViewModel>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		protected abstract BitmapRotation Rotation { get; }

		public bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			(ContentPageContext.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public BaseRotateAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await Task.WhenAll(ContentPageContext.SelectedItems.Select(image => BitmapHelper.RotateAsync(PathNormalization.NormalizePath(image.ItemPath), Rotation)));

			ContentPageContext.ShellPage?.SlimContentPage?.ItemManipulationModel?.RefreshItemsThumbnail();

			await InfoPaneViewModel.UpdateSelectedItemPreviewAsync();
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
				ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
				ContentPageContext.PageType != ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.SelectedItem))
			{
				if (ContentPageContext.ShellPage is not null && ContentPageContext.ShellPage.SlimContentPage is not null)
				{
					var viewModel = ContentPageContext.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
					var extensions = ContentPageContext.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

					viewModel.CheckAllFileExtensions(extensions);
				}

				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
