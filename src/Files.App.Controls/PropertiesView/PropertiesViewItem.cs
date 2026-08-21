// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Text;
using WinRT;

namespace Files.App.Controls.Primitives
{
	public partial class PropertiesViewItem : Control
	{
		private bool _isLoaded;

		public PropertiesViewItem()
		{

			this.Loaded += (s , e) =>
			{
				_isLoaded = true;
				UpdateOrientationState( Orientation );
				UpdateCanEditState( CanEdit );

				// Apply initial state
				UpdateEnabledState( IsEnabled );

				// React to future changes
				RegisterPropertyChangedCallback( Control.IsEnabledProperty , OnIsEnabledChanged );
			};
		}




		#region Property Changed

		[DynamicWindowsRuntimeCast( typeof( Orientation ) )]
		private static void OnOrientationChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItem)d;
			control.UpdateOrientationState( (Orientation)e.NewValue );
		}

		private void UpdateOrientationState(Orientation orientation)
		{
			if ( orientation == Orientation.Horizontal )
			{
				VisualStateManager.GoToState( this , HorizontalStateName , false );
			}
			else
			{
				VisualStateManager.GoToState( this , VerticalStateName , false );
			}
		}




		private static void OnCanEditPropertyChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItem)d;
			control.UpdateCanEditState( (bool)e.NewValue );
		}

		private void UpdateCanEditState(bool canEdit)
		{
			if ( canEdit == true )
			{
				VisualStateManager.GoToState( this , EditableStateName , false );
			}
			else
			{
				VisualStateManager.GoToState( this , NotEditableStateName , false );
			}
		}




		private void OnIsEnabledChanged(DependencyObject sender , DependencyProperty dp)
		{
			UpdateEnabledState( IsEnabled );
		}


		private void UpdateEnabledState(bool isEnabled)
		{
			if ( isEnabled )
				VisualStateManager.GoToState( this , EnabledStateName , true );
			else
				VisualStateManager.GoToState( this , DisabledStateName , true );
		}

		#endregion

	}
}
