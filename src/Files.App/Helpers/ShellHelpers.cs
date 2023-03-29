using Files.App.Extensions;
using System;

namespace Files.App.Helpers
{
	public static class ShellHelpers
	{
		public static string ResolveShellPath(string shPath)
		{
			if (shPath.StartsWith(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				return CommonPaths.RecycleBinPath;

			if (shPath.StartsWith(CommonPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
				return CommonPaths.MyComputerPath;

			if (shPath.StartsWith(CommonPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
				return CommonPaths.NetworkFolderPath;

			return shPath;
		}

		public static string GetShellNameFromPath(string shPath)
		{
			return shPath switch
			{
				"Home" => "Home".GetLocalizedResource(),
				CommonPaths.RecycleBinPath => "RecycleBin".GetLocalizedResource(),
				CommonPaths.NetworkFolderPath => "SidebarNetworkDrives".GetLocalizedResource(),
				CommonPaths.MyComputerPath => "ThisPC".GetLocalizedResource(),
				_ => shPath
			};
		}
	}
}
