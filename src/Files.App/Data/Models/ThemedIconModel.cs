// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
