// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.UserControls
{
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
		/// Initializes a new instance of the <see cref="SettingsExpanderItemStyleSelector"/> class.
		/// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public SettingsExpanderItemStyleSelector()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
		}

		/// <inheritdoc/>
		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			if (container is SettingsCard card && card.IsClickEnabled)
			{
				return ClickableStyle;
			}
			else
			{
				return DefaultStyle;
			}
		}
	}
}
