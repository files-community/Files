using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class ElevationHelpers
	{
		[DllImport("shell32.dll", EntryPoint = "#865", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)] private static extern bool _IsElevationRequired([MarshalAs(UnmanagedType.LPWStr)] string pszPath);

		public static bool IsElevationRequired(string filePath)
		{
			if (string.IsNullOrEmpty(filePath))
				return false;

			return _IsElevationRequired(filePath);
		}
	}
}