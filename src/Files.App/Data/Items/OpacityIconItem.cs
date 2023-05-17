// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Items
{
	public struct OpacityIconItem
	{
		public string OpacityIconStyle { get; init; }

		public readonly OpacityIcon ToOpacityIcon()
		{
			return new()
			{
				Style = (Style)Application.Current.Resources[OpacityIconStyle],
			};
		}

		public readonly bool IsValid
			=> !string.IsNullOrEmpty(OpacityIconStyle);
	}
}
