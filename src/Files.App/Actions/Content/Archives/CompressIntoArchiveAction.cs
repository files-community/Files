using Files.App.Contexts;
using Files.App.Dialogs;
using Files.App.Filesystem.Archive;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions
{
	internal class CompressIntoArchiveAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "CreateArchive".GetLocalizedResource();

		public string Description => "CompressIntoArchiveDescription".GetLocalizedResource();

		public override bool IsExecutable => 
			IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanCompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public CompressIntoArchiveAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var (sources, directory, fileName) = ArchiveHelpers.GetCompressDestination(context.ShellPage);

			var dialog = new CreateArchiveDialog
			{
				FileName = fileName,
			};
			var result = await dialog.TryShowAsync();

			if (!dialog.CanCreate || result != ContentDialogResult.Primary)
				return;

			IArchiveCreator creator = new ArchiveCreator
			{
				Sources = sources,
				Directory = directory,
				FileName = dialog.FileName,
				Password = dialog.Password,
				FileFormat = dialog.FileFormat,
				CompressionLevel = dialog.CompressionLevel,
				SplittingSize = dialog.SplittingSize,
			};

			await ArchiveHelpers.CompressArchiveAsync(creator);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}