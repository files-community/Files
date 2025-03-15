// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using System.Linq;
using System.Collections.Generic;

namespace Files.App.Controls
{
	public partial class Omnibar
	{
		[GeneratedDependencyProperty]
		public partial IList<OmnibarMode>? Modes { get; set; }

		[GeneratedDependencyProperty]
		public partial OmnibarMode? CurrentActiveMode { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? DefaultInactiveMode { get; set; }

		partial void OnDefaultInactiveModeChanged(FrameworkElement? newValue)
		{
			if (Modes is null)
				return;

			foreach (var mode in Modes)
			{
				//if (mode.UseDefaultInactiveMode)
				//	mode.ContentOnInactive = newValue;
			}
		}
	}
}
