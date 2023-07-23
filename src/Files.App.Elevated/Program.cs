using Files.Core.Data.Items;
using Files.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace Files.App.Elevated
{
	public class Program
	{
		public static FileLogger Logger { get; private set; }

		[STAThread]
		private static async Task Main(string[] args)
		{
			if (args is null || args.Length < 2)
				return;

			Logger = new FileLogger(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug_fulltrust.log"));
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

			switch (args[0])
			{
				case "FileOperation":
					await HandleFileOperation(args[1]);
					break;
			}
		}

		private static async Task HandleFileOperation(string operationID)
		{
			using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(operationID, MemoryMappedFileRights.Read, HandleInheritability.Inheritable))
			{
				using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					var ros = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
					Logger.LogInformation(ros);
					var req = JsonSerializer.Deserialize<ShellOperationRequest>(ros);
					switch (req.Operation)
					{
						case OperationType.Copy when req is ShellOperationCopyMoveRequest cm:
							await FileOperationsHelpers.CopyItemAsync(cm.Sources, cm.Destinations, cm.Replace, 0, req.ID);
							break;
						case OperationType.Move when req is ShellOperationCopyMoveRequest cm:
							await FileOperationsHelpers.MoveItemAsync(cm.Sources, cm.Destinations, cm.Replace, 0, req.ID);
							break;
						case OperationType.Delete when req is ShellOperationDeleteRequest del:
							await FileOperationsHelpers.DeleteItemAsync(del.Sources, del.Permanently, 0, req.ID);
							break;
						case OperationType.Rename when req is ShellOperationRenameRequest rn:
							await FileOperationsHelpers.RenameItemAsync(rn.Source, rn.Destination, rn.Replace);
							break;
						case OperationType.Create when req is ShellOperationCreateRequest cr:
							await FileOperationsHelpers.CreateItemAsync(cr.Source, cr.CreateOption, cr.Template, cr.Data);
							break;
					}
				}
			}
		}

		private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;
			Logger.LogError(exception, exception.Message);
		}
	}
}