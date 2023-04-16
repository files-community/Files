using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Threading;

namespace Files.App.DataModels
{
	public class PropertiesPageNavigationParameter
	{
		public CancellationTokenSource CancellationTokenSource;

		public object Parameter;

		public IShellPage AppInstance;

		public Window Window;

		public AppWindow AppWindow;
	}
}
