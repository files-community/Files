// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.Resources;

namespace Files.App.Helpers
{
	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public sealed class ResourceString : MarkupExtension
	{
		private static readonly ResourceLoader _resourceLoader = new();

		public string Name { get; set; } = string.Empty;

		protected override object ProvideValue()
		{
			return _resourceLoader.GetString(Name);
		}
	}
}
