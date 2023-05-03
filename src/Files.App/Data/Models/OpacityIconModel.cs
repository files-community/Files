// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls;
using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	public class OpacityIconModel
	{
		public string? OpacityIconStyle { get; set; }

		public OpacityIcon ToOpacityIcon() => new()
		{
			Style = (Style)Application.Current.Resources[OpacityIconStyle],
		};

		public bool IsValid
			=> !string.IsNullOrEmpty(OpacityIconStyle);
	}
}
