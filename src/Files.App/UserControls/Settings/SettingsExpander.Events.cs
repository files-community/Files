// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Files.App.UserControls.Settings
{
	public partial class SettingsExpander
	{
		/// <summary>
		/// Fires when the SettingsExpander is opened
		/// </summary>
		public event EventHandler? Expanded;

		/// <summary>
		/// Fires when the expander is closed
		/// </summary>
		public event EventHandler? Collapsed;
	}
}
