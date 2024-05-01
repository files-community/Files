// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	public class OpacityIconModel
	{
		public string OpacityIconStyle { get; set; }

		public bool IsValid
			=> !string.IsNullOrEmpty(OpacityIconStyle);

		public OpacityIconModel(string styleName = "")
		{
			OpacityIconStyle = styleName;
		}

		public OpacityIcon ToOpacityIcon()
		{
			return new()
			{
				Style = (Style)Application.Current.Resources[OpacityIconStyle],
			};
		}
	}
}
