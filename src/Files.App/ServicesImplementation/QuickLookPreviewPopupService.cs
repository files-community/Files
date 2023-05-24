// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;

namespace Files.App.ServicesImplementation
{
	/// <inheritdoc cref="IPreviewPopupService"/>
	internal sealed class QuickLookPreviewPopupService : ObservableObject, IPreviewPopupService
	{
		private const int TIMEOUT = 500;
		private static string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
		private static string pipeMessageSwitch = "QuickLook.App.PipeMessages.Switch";
		private static string pipeMessageToggle = "QuickLook.App.PipeMessages.Toggle";

		public async Task OpenPreviewPopup(string path, bool switchPreview = false)
		{
			bool isQuickLookAvailable = await DetectAvailability();

			if (!isQuickLookAvailable)
				return;

			string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
			string message = switchPreview ? pipeMessageSwitch : pipeMessageToggle;

			await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
			try
			{
				await client.ConnectAsync(TIMEOUT);

				await using var writer = new StreamWriter(client);
				await writer.WriteLineAsync($"{message}|{path}");
				await writer.FlushAsync();
			}
			catch (TimeoutException)
			{
				client.Close();
			}
		}

		public async Task<bool> DetectAvailability()
		{
			async Task<int> QuickLookServerAvailable()
			{
				await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
				try
				{
					await client.ConnectAsync(TIMEOUT);
					var serverInstances = client.NumberOfServerInstances;

					await using var writer = new StreamWriter(client);
					await writer.WriteLineAsync($"{pipeMessageSwitch}|");
					await writer.FlushAsync();

					return serverInstances;
				}
				catch (TimeoutException)
				{
					client.Close();
					return 0;
				}
			}

			try
			{
				var result = await QuickLookServerAvailable();
				return result != 0;
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, ex.Message);
				return false;
			}
		}
	}
}
