// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Settings
{
	/// <summary>
	/// <see cref="StyleSelector"/> used by <see cref="SettingsExpander"/> to choose the proper <see cref="SettingsCard"/> container style (clickable or not).
	/// </summary>
	public class SettingsExpanderItemStyleSelector : StyleSelector
	{
		/// <summary>
		/// Gets or sets the default <see cref="Style"/>.
		/// </summary>
		public Style DefaultStyle { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Style"/> when clickable.
		/// </summary>
		public Style ClickableStyle { get; set; }

		/// <summary>
		/// Gets or sets the unknown <see cref="Style"/>.
		/// </summary>
		public Style UnknownStyle { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsExpanderItemStyleSelector"/> class.
		/// </summary>
		public SettingsExpanderItemStyleSelector()
		{
		}

		/// <inheritdoc/>
		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			if (container is SettingsCard card)
			{
				if (card.IsClickEnabled)
					return ClickableStyle;
				else
					return DefaultStyle;
			}
			else
				return UnknownStyle;
		}
	}
}
