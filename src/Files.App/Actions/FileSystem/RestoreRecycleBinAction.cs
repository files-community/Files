// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class RestoreRecycleBinAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Restore".GetLocalizedResource();

		public string Description
			=> "RestoreRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRestoreItem");

		public override bool IsExecutable =>
			context.PageType is ContentPageTypes.RecycleBin &&
			context.SelectedItems.Any() &&
			UIHelpers.CanShowDialog;

		public RestoreRecycleBinAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return;

			var selectedItems = context.ShellPage.SlimContentPage.SelectedItems;
			if (selectedItems == null)
				return;

			var confirmEmptyBinDialog = new ContentDialog()
			{
				Title = "ConfirmRestoreSelectionBinDialogTitle".GetLocalizedResource(),

				Content = string.Format("ConfirmRestoreSelectionBinDialogContent".GetLocalizedResource(), selectedItems.Count),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = MainWindow.Instance.Content.XamlRoot
			};

			ContentDialogResult result = await confirmEmptyBinDialog.TryShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				var selected = context.ShellPage.SlimContentPage.SelectedItems;
				if (selected == null)
					return;

				var items = selected.ToList().Where(x => x is RecycleBinItem).Select((item) => new
				{
					Source = StorageHelpers.FromPathAndType(
						item.ItemPath,
						item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory),
					Dest = ((RecycleBinItem)item).ItemOriginalPath
				});

				await context.ShellPage.FilesystemHelpers.RestoreItemsFromTrashAsync(items.Select(x => x.Source), items.Select(x => x.Dest), true);
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItems):
					if (context.PageType is ContentPageTypes.RecycleBin)
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
