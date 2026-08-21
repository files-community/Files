// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
using Files.App.Controls.Primitives;

namespace Files.App.Controls
{
	public sealed partial class PropertiesViewItemText : PropertiesViewItem
	{


		#region Properties

		public string Text
		{
			get { return (string)GetValue( TextProperty ); }
			set { SetValue( TextProperty , value ); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(PropertiesViewItemText),
				new PropertyMetadata( string.Empty ) );

		#endregion


		public PropertiesViewItemText()
		{
			DefaultStyleKey = typeof( PropertiesViewItemText );
		}
	}
}
