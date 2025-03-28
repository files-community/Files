// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Markup;

namespace Files.App.Controls
{
	[MarkupExtensionReturnType(ReturnType = typeof(ThemedIcon))]
	public sealed partial class ThemedIconMarkup : MarkupExtension
	{
		public Style Style { get; set; } = null!;

		public bool IsFilled { get; set; }

		public ThemedIconTypes IconType { get; set; }

		protected override object ProvideValue()
		{
			return new ThemedIcon() { Style = Style, IsFilled = IsFilled, IconType = IconType };
		}
	}
}
