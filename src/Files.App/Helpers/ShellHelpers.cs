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
	}
}
