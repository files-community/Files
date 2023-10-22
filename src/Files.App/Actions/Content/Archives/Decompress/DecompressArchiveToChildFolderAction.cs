// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class DecompressArchiveToChildFolderAction : BaseDecompressArchiveAction
	{
		public override string Label
			=> ComputeLabel();

		public override string Description
			=> "DecompressArchiveToChildFolderDescription".GetLocalizedResource();

		public DecompressArchiveToChildFolderAction()
		{
		}

		public override Task ExecuteAsync()
		{
			return DecompressHelper.DecompressArchiveToChildFolderAsync(context.ShellPage);
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					{
						if (IsContextPageTypeAdaptedToCommand())
						{
							OnPropertyChanged(nameof(Label));
							OnPropertyChanged(nameof(IsExecutable));
						}

						break;
					}
			}
		}

		private string ComputeLabel()
		{
			if (context.SelectedItems == null || context.SelectedItems.Count == 0)
				return string.Empty;

			return context.SelectedItems.Count > 1
				? string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), "*")
				: string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), SystemIO.Path.GetFileNameWithoutExtension(context.SelectedItems.First().Name));
		}
	}
}
