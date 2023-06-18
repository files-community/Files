// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;
using Files.App.Shell;
using Microsoft.Win32;

namespace Files.App.Actions
{
	internal class OpenInVSCodeAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		private readonly bool _isVSCodeInstalled;

		public string Label { get; } = "OpenInVSCode".GetLocalizedResource();

		public string Description { get; } = "OpenInVSCodeDescription".GetLocalizedResource();

		public bool IsExecutable =>
			_isVSCodeInstalled &&
			_context.Folder is not null;

		public OpenInVSCodeAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_isVSCodeInstalled = IsVSCodeInstalled();
			if (_isVSCodeInstalled)
				_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			Win32API.RunPowershellCommand($"code {_context.ShellPage?.FilesystemViewModel.WorkingDirectory}", false);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.Folder))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private static bool IsVSCodeInstalled()
		{
			string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

			var key = Registry.CurrentUser.OpenSubKey(registryKey);
			if (key is null)
				return false;

			string? displayName;

			foreach (var subKey in key.GetSubKeyNames().Select(key.OpenSubKey))
			{
				displayName = subKey?.GetValue("DisplayName") as string;
				if (!string.IsNullOrWhiteSpace(displayName) && displayName.StartsWith("Microsoft Visual Studio Code"))
				{
					key.Close();

					return true;
				}
			}

			key.Close();

			return false;
		}
	}
}
