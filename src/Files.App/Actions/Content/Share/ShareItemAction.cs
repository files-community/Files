// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal class ShareItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Share".GetLocalizedResource();

		public string Description
			=> "ShareItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconShare");

		public bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			DataTransferManager.IsSupported() &&
			ContentPageContext.SelectedItems.Any() &&
			ContentPageContext.SelectedItems.All(ShareItemHelpers.IsItemShareable);

		public ShareItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return ShareItemHelpers.ShareItemsAsync(ContentPageContext.SelectedItems);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
				ContentPageContext.PageType != ContentPageTypes.Home &&
				ContentPageContext.PageType != ContentPageTypes.Ftp &&
				ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
				ContentPageContext.PageType != ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
