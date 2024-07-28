// Copyright (c) 2024 Files Community
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

namespace Files.App.Controls
{
	public partial class MenuFlyoutItemEx : MenuFlyoutItem
	{
		private bool _useThemedIcon;
		private ThemedIcon _tIcon;
		private Border _tIconRoot;



		public MenuFlyoutItemEx()
		{
			this.DefaultStyleKey = typeof( MenuFlyoutItemEx );
		}



		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			this._tIcon = GetTemplateChild( ThemedIconPartName ) as ThemedIcon;
		}



		private void ThemedIconChanged(DependencyObject d, Style newStyle)
		{
			var control = (MenuFlyoutItemEx)d;

			// Handles changes to the ThemedIcon's Style

			if ( control._tIcon != null && control._tIconRoot != null )
			{
			}
		}



		private void IconSizeChanged(DependencyObject d , double newValue)
		{
			// Handle IconSize changes
		}
	}
}
