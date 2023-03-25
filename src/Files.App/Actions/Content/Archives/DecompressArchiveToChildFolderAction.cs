﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DecompressArchiveToChildFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => ComputeLabel();

		public string Description => "TODO: Need to be described.";

		public bool IsExecutable => IsContextPageTypeAdaptedToCommand()
									&& ArchiveHelpers.CanDecompress(context.SelectedItems);

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
