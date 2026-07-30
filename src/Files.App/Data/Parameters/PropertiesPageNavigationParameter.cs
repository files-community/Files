// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Parameters
{
	public sealed class PropertiesPageNavigationParameter
	{
		public CancellationTokenSource CancellationTokenSource = new();

		public required object Parameter;

		public required IShellPage AppInstance;

		public required Window Window;
	}
}
