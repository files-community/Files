// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Controls;
using Microsoft.UI.Xaml;

namespace Files.App.Data.Models
{
	public struct ThemedIconModel
	{
		public string ThemedIconStyle { get; set; }

		public readonly ThemedIcon ToThemedIcon()
		{
			return new()
			{
				Style = (Style)Application.Current.Resources[ThemedIconStyle],
			};
		}

		public readonly bool IsValid
			=> !string.IsNullOrEmpty(ThemedIconStyle);
	}
}
