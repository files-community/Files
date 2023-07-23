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
