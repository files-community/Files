// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;

namespace Files.App.Extensions
{
	/// <summary>
	/// Associates elements of <see cref="DetailsLayoutPage"/> with the column they belong to.
	/// </summary>
	public sealed class DetailsLayoutColumnExtensions : DependencyObject
	{
		public static string? GetId(DependencyObject obj)
		{
			return obj.GetValue(IdProperty) as string;
		}

		public static void SetId(DependencyObject obj, string? value)
		{
			obj.SetValue(IdProperty, value);
		}

		public static readonly DependencyProperty IdProperty =
			DependencyProperty.RegisterAttached(
				"Id",
				typeof(string),
				typeof(DetailsLayoutColumnExtensions),
				new PropertyMetadata(null));
	}
}
