// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal abstract class BaseRotateAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly InfoPaneViewModel _infoPaneViewModel;

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		protected abstract BitmapRotation Rotation { get; }

		public bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			(context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public BaseRotateAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_infoPaneViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (var image in context.SelectedItems)
				await BitmapHelper.RotateAsync(PathNormalization.NormalizePath(image.ItemPath), Rotation);

			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.RefreshItemsThumbnail();

			await _infoPaneViewModel.UpdateSelectedItemPreviewAsync();
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
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
