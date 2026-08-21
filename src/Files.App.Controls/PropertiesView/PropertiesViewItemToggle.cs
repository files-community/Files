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
using WinRT;

namespace Files.App.Controls
{
	#region Template Parts

	[TemplatePart( Name = ToggleSwitchPartName , Type = typeof( ToggleSwitch ) )]

	#endregion



	public sealed partial class PropertiesViewItemToggle : PropertiesViewItem
	{
		#region Constants
		internal const string ToggleSwitchPartName = "PART_ToggleSwitch";

		#endregion




		#region Properties

		public bool IsToggled
		{
			get { return (bool)GetValue( IsToggledProperty ); }
			set { SetValue( IsToggledProperty , value ); }
		}

		public static readonly DependencyProperty IsToggledProperty =
			DependencyProperty.Register(
				"IsToggled",
				typeof(bool),
				typeof(PropertiesViewItemToggle),
				new PropertyMetadata( false , OnIsToggledPropertyChanged ) );



		public string OnText
		{
			get { return (string)GetValue( OnTextProperty ); }
			set { SetValue( OnTextProperty , value ); }
		}

		public static readonly DependencyProperty OnTextProperty =
			DependencyProperty.Register(
				"OnText",
				typeof(string),
				typeof(PropertiesViewItemToggle),
				new PropertyMetadata( "On" ) );



		public string OffText
		{
			get { return (string)GetValue( OffTextProperty ); }
			set { SetValue( OffTextProperty , value ); }
		}

		public static readonly DependencyProperty OffTextProperty =
			DependencyProperty.Register(
				"OffText",
				typeof(string),
				typeof(PropertiesViewItemToggle),
				new PropertyMetadata( "Off") );

		#endregion



		private ToggleSwitch? _toggleSwitch;

		#region Property Changed

		private static void OnIsToggledPropertyChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItemToggle)d;
			control.UpdateIsToggledProperty( (bool)e.NewValue );

			if ( e.NewValue != e.OldValue )
			{ 
				control.UpdateIsToggledProperty( (bool) e.NewValue );
			}
		}

		private void UpdateIsToggledProperty(bool isToggled)
		{
			if ( _toggleSwitch == null )
				return;

			_toggleSwitch.IsOn = isToggled;
		}

		#endregion


		private bool _isLoaded;
		public PropertiesViewItemToggle()
		{
			DefaultStyleKey = typeof( PropertiesViewItemToggle );

			this.Loaded += (s , e) =>
			{
				_isLoaded = true;
				UpdateIsToggledProperty( (bool)IsToggled );
			};
		}

		[DynamicWindowsRuntimeCast( typeof( ToggleSwitch ) )]
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Renive IsOn event hook if ToggleSwitch Part is not found or loaded
			if ( _toggleSwitch != null )
			{
				_toggleSwitch.Toggled -= OnToggleSwitchToggled;
			}

			// Get the template part
			_toggleSwitch = GetTemplateChild( ToggleSwitchPartName ) as ToggleSwitch;

			// Hook the event
			if ( _toggleSwitch != null )
			{
				_toggleSwitch.Toggled += OnToggleSwitchToggled;
			}
		}




		private void OnToggleSwitchToggled(object sender , RoutedEventArgs e)
		{
			if ( _toggleSwitch == null )
				return;

			bool isOn = _toggleSwitch.IsOn;

			this.IsToggled = isOn;
		}

	}
}
