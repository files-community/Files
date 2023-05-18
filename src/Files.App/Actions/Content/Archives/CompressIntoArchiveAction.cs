// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;
using Files.App.Dialogs;
using Files.App.Filesystem.Archive;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions
{
	internal class CompressIntoArchiveAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly CreateArchiveDialogViewModel viewModel = new();

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

			viewModel.FileName = fileName;

			var result = await dialogService.ShowDialogAsync(viewModel);

			if (!viewModel.CanCreate || result != DialogResult.Primary)
				return;

			IArchiveCreator creator = new ArchiveCreator
			{
				Sources = sources,
				Directory = directory,
				FileName = viewModel.FileName,
				Password = viewModel.Password,
				FileFormat = viewModel.FileFormat.Key,
				CompressionLevel = viewModel.CompressionLevel.Key,
				SplittingSize = viewModel.SplittingSize.Key,
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