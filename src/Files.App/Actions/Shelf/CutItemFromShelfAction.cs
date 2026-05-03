// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CutItemFromShelfAction : ObservableObject, IAction
	{
		private readonly IShelfContext shelfContext;
		private readonly IContentPageContext contentPageContext;
		private readonly StatusCenterViewModel statusCenterViewModel;

		public string Label
			=> Strings.Cut.GetLocalizedResource();

		public string Description
			=> Strings.CutItemDescription.GetLocalizedFormatResource(shelfContext.SelectedItems.Count);

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Cut");

		public bool IsExecutable
			=> shelfContext.HasSelection && contentPageContext.ShellPage?.ShellViewModel is not null;

		public bool IsAccessibleGlobally
			=> false;

		public CutItemFromShelfAction()
		{
			shelfContext = Ioc.Default.GetRequiredService<IShelfContext>();
			contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			shelfContext.PropertyChanged += ShelfContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (contentPageContext.ShellPage?.ShellViewModel is not { } shellViewModel)
				return;

			var items = shelfContext.SelectedItems.Select(x => x.Inner).ToArray();
			await TransferHelpers.ExecuteTransferAsync(items, shellViewModel, statusCenterViewModel, DataPackageOperation.Move);
		}

		private void ShelfContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IShelfContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
