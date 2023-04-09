using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem.Archive;
using Files.App.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CompressIntoArchiveAction : BaseUIAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override string Label => "CreateArchive".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override bool IsExecutable => 
			IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanCompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public CompressIntoArchiveAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public override async Task ExecuteAsync()
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