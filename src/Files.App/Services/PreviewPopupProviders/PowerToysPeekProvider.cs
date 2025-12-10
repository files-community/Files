// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Services.PreviewPopupProviders
{
	public sealed class PowerToysPeekProvider : IPreviewPopupProvider
	{
		public static PowerToysPeekProvider Instance { get; } = new();

		private static string? _peekExecutablePath;

		public async Task TogglePreviewPopupAsync(string path)
		{
			await DoPreviewAsync(path);
		}

		public async Task SwitchPreviewAsync(string path)
		{
			// Not used
		}

		private async Task DoPreviewAsync(string path)
		{
			if (_peekExecutablePath != null)
			{
				try
				{
					var psi = new ProcessStartInfo
					{
						FileName = _peekExecutablePath,
						Arguments = $"\"{path}\"",
						UseShellExecute = true,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					};

					Process.Start(psi);
				}
				catch
				{
					// Ignore
				}
			}
		}

		public async Task<bool> DetectAvailability()
		{
			var exeName = "PowerToys.Peek.UI.exe";
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var perUserPath = Path.Combine(localAppData, "PowerToys", "WinUI3Apps", exeName);

			// User path
			if (File.Exists(perUserPath))
			{
				_peekExecutablePath = perUserPath;
				return true;
			}

			// Machine-wide path
			string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string machinePath = Path.Combine(programFiles, "PowerToys", "WinUI3Apps", exeName);

			if (File.Exists(machinePath))
			{
				_peekExecutablePath = machinePath;
				return true;
			}

			// Not found
			return false;
		}
	}
}
