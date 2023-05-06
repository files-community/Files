// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;
using System.IO;

namespace Files.App.Actions
{
	internal class DecompressArchiveToChildFolderAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => ComputeLabel();

		public string Description => "DecompressArchiveToChildFolderDescription".GetLocalizedResource();

		public override bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanDecompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public DecompressArchiveToChildFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ArchiveHelpers.DecompressArchiveToChildFolder(context.ShellPage);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private string ComputeLabel()
		{
			if (context.SelectedItems == null || context.SelectedItems.Count == 0)
				return string.Empty;

			return context.SelectedItems.Count > 1
				? string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), "*")
				: string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), Path.GetFileNameWithoutExtension(context.SelectedItems.First().Name));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
					{
						OnPropertyChanged(nameof(Label));
						OnPropertyChanged(nameof(IsExecutable));
					}
					break;
			}
		}
	}
}
