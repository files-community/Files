// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal abstract class BaseRotateAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		protected abstract BitmapRotation Rotation { get; }

		public bool IsExecutable => IsContextPageTypeAdaptedToCommand() &&
			(context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public BaseRotateAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (var image in context.SelectedItems)
				await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), Rotation);

			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.RefreshItemsThumbnail();
			Ioc.Default.GetRequiredService<PreviewPaneViewModel>().UpdateSelectedItemPreview();
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.SelectedItem))
			{
				if (context.ShellPage is not null && context.ShellPage.SlimContentPage is not null)
				{
					var viewModel = context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
					var extensions = context.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

					viewModel.CheckAllFileExtensions(extensions);
				}

				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
