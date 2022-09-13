using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Files.App.Helpers;

public static class QuickLookHelpers
{
    public static Task ToggleQuickLook(IShellPage associatedInstance, bool switchPreview = false)
    {
        if (!App.AppModel.IsQuickLookSupported || !associatedInstance.SlimContentPage.IsItemSelected || associatedInstance.SlimContentPage.IsRenamingItem)
            return Task.CompletedTask;

        App.Logger.Info("Toggle QuickLook");

        string PipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
        string Message = switchPreview ? "QuickLook.App.PipeMessages.Switch" : "QuickLook.App.PipeMessages.Toggle";

        using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
        client.Connect();

        using var writer = new StreamWriter(client);
        writer.WriteLine($"{Message}|{associatedInstance.SlimContentPage.SelectedItem.ItemPath}");
        writer.Flush();
        return Task.CompletedTask;
    }

    public static bool CheckQuickLookAvailability()
    {
        static int QuickLookServerAvailable()
        {
            string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
            string pipeSwitch = "QuickLook.App.PipeMessages.Switch";

            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            try
            {
                client.Connect(500);
                var serverInstances = client.NumberOfServerInstances;

                using var writer = new StreamWriter(client);
                writer.WriteLine($"{pipeSwitch}|");
                writer.Flush();

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
            var result = QuickLookServerAvailable();
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