// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Parameters
{
	public sealed class PropertiesPageNavigationParameter
	{
		public CancellationTokenSource CancellationTokenSource;

		public object Parameter;

		public IShellPage AppInstance;

		public Window Window;
	}
}
