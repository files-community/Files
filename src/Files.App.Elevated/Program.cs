using Files.Shared;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace Files.App.Elevated
{
	public class Program
	{
		public static FileLogger Logger { get; private set; }

		[STAThread]
		private static void Main(string[] args)
		{
			if (args is null || args.Length != 2)
				return;

			Logger = new FileLogger(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug_fulltrust.log"));
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

			switch (args[1])
			{
				case "FileOperation":
					HandleFileOperation(args[1]);
					break;
			}
		}

		private static void HandleFileOperation(string args)
		{
			Logger.LogInformation(args);
		}

		private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;
			Logger.LogError(exception, exception.Message);
		}
	}
}