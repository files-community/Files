// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.ViewModels.Dialogs.FileSystemDialog;
using Microsoft.UI.Xaml;

namespace Files.App.TemplateSelectors
{
	internal sealed class FileSystemDialogItemSelector : BaseTemplateSelector<BaseFileSystemDialogItemViewModel>
	{
		public DataTemplate? ConflictItemDataTemplate { get; set; }

		public DataTemplate? DefaultItemDataTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(BaseFileSystemDialogItemViewModel? item, DependencyObject container)
		{
			if (item is FileSystemDialogConflictItemViewModel)
			{
				return ConflictItemDataTemplate!;
			}
			else if (item is FileSystemDialogDefaultItemViewModel)
			{
				return DefaultItemDataTemplate!;
			}
			else
			{
				return base.SelectTemplateCore(item, container);
			}
		}
	}
}
