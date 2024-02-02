// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	public struct OpacityIconModel
	{
		public string OpacityIconStyle { get; set; }

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
