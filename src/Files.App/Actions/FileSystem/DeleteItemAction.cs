using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.Backend.Services.Settings;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal class DeleteItemAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label { get; } = "Delete".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public HotKey HotKey { get; } = new(VirtualKey.Delete);

		public override bool IsExecutable =>
			context.HasSelection &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false) &&
			UIHelpers.CanShowDialog;

		public DeleteItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is null || !IsExecutable)
				return;

			var items = context.SelectedItems.Select(item => StorageHelpers.FromPathAndType(item.ItemPath,
					item.PrimaryItemAttribute is StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));

			await context.ShellPage.FilesystemHelpers.DeleteItemsAsync(items, settings.DeleteConfirmationPolicy, false, true);
			await context.ShellPage.FilesystemViewModel.ApplyFilesAndFoldersChangesAsync();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
