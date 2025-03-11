// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed partial class RunWithPowershellAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.RunWithPowerShell.GetLocalizedResource();

		public string Description
			=> Strings.RunWithPowershellDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE756");

		public bool IsExecutable =>
			context.SelectedItem is not null &&
			FileExtensionHelpers.IsPowerShellFile(context.SelectedItem.FileExtension);

		public RunWithPowershellAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.RunPowershellCommandAsync(
				$"& '{context.ShellPage?.SlimContentPage?.SelectedItem?.ItemPath}'",
				PowerShellExecutionOptions.None,
				context.Folder?.ItemPath
			);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
