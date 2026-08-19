// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Markup;

namespace Files.App.Helpers
{
	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public sealed partial class ResourceString : MarkupExtension
	{
		public string Name { get; set; } = string.Empty;

		protected override object ProvideValue() => Name.GetLocalizedResource();
	}
}
