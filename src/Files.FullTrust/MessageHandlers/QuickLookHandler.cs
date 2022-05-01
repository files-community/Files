using Files.Shared;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class QuickLookHandler : Disposable, IMessageHandler
    {
        private static readonly ILogger Logger = Program.Logger;

        public void Initialize(PipeStream connection)
        {
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "DetectQuickLook":
                    // Check QuickLook Availability
                    var available = CheckQuickLookAvailability();
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "IsAvailable", available } }, message.Get("RequestID", (string)null));
                    break;

                case "ToggleQuickLook":
                    var path = (string)message["path"];
                    var switchPreview = (bool)message["switch"];
                    SafetyExtensions.IgnoreExceptions(() => ToggleQuickLook(path, switchPreview), Logger);
                    break;
            }
        }

        private static void ToggleQuickLook(string path, bool switchPreview)
        {
            Logger.Info("Toggle QuickLook");

            string PipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
            string Message = switchPreview ? "QuickLook.App.PipeMessages.Switch" : "QuickLook.App.PipeMessages.Toggle";

            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect();

            using var writer = new StreamWriter(client);
            writer.WriteLine($"{Message}|{path}");
            writer.Flush();
        }

        private static bool CheckQuickLookAvailability()
        {
            static int QuickLookServerAvailable()
            {
                string PipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
                string Switch = "QuickLook.App.PipeMessages.Switch";

                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                try
                {
                    client.Connect(500);
                    var serverInstances = client.NumberOfServerInstances;

                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine($"{Switch}|");
                        writer.Flush();
                    }

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
                Logger.Info($"QuickLook detected: {result != 0}");
                return result != 0;
            }
            catch (Exception ex)
            {
                Logger.Info(ex, ex.Message);
                return false;
            }
        }
    }
}