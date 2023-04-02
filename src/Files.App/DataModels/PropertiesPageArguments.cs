using System.Threading;

namespace Files.App.DataModels
{
	public class PropertiesPageArguments
	{
		public CancellationTokenSource CancellationTokenSource;

		public object Parameter;

		public IShellPage AppInstance;
	}
}
