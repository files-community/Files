// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a model for <see cref="OpacityIcon"/>.
	/// </summary>
	public class OpacityIconModel
	{
		public string? OpacityIconStyle { get; set; }

		public bool IsValid
			=> !string.IsNullOrWhiteSpace(OpacityIconStyle);

		public OpacityIconModel(string styleName)
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
