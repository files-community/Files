// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.Settings
{
	internal class ControlHelpers
	{
		internal static bool IsXamlRootAvailable { get; } =
			Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");
	}
}
