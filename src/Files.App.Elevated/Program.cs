// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Items;
using Files.Shared;
using Microsoft.Extensions.Logging;
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
		private static async Task<int> Main(string[] args)
		{
			if (args is null || args.Length < 2)
				return 0;

			Logger = new FileLogger(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug_fulltrust.log"));
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

			switch (args[0])
			{
				case "FileOperation":
					var (success, res) = await HandleFileOperation(args[1]);
					return success && res.Final.All(x => x.Succeeded) ? 0 : -1;
			}

			return 0;
		}

		private static async Task<(bool, ShellOperationResult)> HandleFileOperation(string operationID)
		{
			using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(operationID, MemoryMappedFileRights.Read, HandleInheritability.Inheritable))
			{
				using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					var ros = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
					Logger.LogInformation(ros);
					var req = JsonSerializer.Deserialize<ShellOperationRequest>(ros);
					switch (req?.Operation)
					{
						case OperationType.Copy when req is ShellOperationCopyMoveRequest cm:
							return await FileOperationsHelpers.CopyItemAsync(cm.Sources, cm.Destinations, cm.Replace, 0, req.ID);
						case OperationType.Move when req is ShellOperationCopyMoveRequest cm:
							return await FileOperationsHelpers.MoveItemAsync(cm.Sources, cm.Destinations, cm.Replace, 0, req.ID);
						case OperationType.Delete when req is ShellOperationDeleteRequest del:
							return await FileOperationsHelpers.DeleteItemAsync(del.Sources, del.Permanently, 0, req.ID);
						case OperationType.Rename when req is ShellOperationRenameRequest rn:
							return await FileOperationsHelpers.RenameItemAsync(rn.Source, rn.Destination, rn.Replace);
						case OperationType.Create when req is ShellOperationCreateRequest cr:
							return await FileOperationsHelpers.CreateItemAsync(cr.Source, cr.CreateOption, cr.Template, cr.Data);
						default:
							return (false, new());
					}
				}
			}
		}

		private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = (Exception)e.ExceptionObject;
			Logger.LogError(ex, ex.Message);
		}
	}
}