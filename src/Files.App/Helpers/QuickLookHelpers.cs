using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Files.App.Helpers;

public static class QuickLookHelpers
{
	private const int TIMEOUT = 500;

	public static async Task ToggleQuickLook(string path, bool switchPreview = false)
	{
		App.AppModel.IsQuickLookAvailable = await DetectQuickLookAvailability();

		if (App.AppModel.IsQuickLookAvailable == false)
			return;

		string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
		string message = switchPreview ? "QuickLook.App.PipeMessages.Switch" : "QuickLook.App.PipeMessages.Toggle";

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

	private static async Task<bool> DetectQuickLookAvailability()
	{
		static async Task<int> QuickLookServerAvailable()
		{
			string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
			string pipeSwitch = "QuickLook.App.PipeMessages.Switch";

			await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
			try
			{
				await client.ConnectAsync(TIMEOUT);
				var serverInstances = client.NumberOfServerInstances;

				await using var writer = new StreamWriter(client);
				await writer.WriteLineAsync($"{pipeSwitch}|");
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
			App.Logger.Info($"QuickLook detected: {result != 0}");
			return result != 0;
		}
		catch (Exception ex)
		{
			App.Logger.Info(ex, ex.Message);
			return false;
		}
	}
}
