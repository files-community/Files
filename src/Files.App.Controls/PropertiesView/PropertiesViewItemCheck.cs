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
	#region Template

	[TemplateVisualState( Name = HorizontalStateName , GroupName = OrientationStateGroupName )]
	[TemplateVisualState( Name = VerticalStateName , GroupName = OrientationStateGroupName )]

	#endregion




	public sealed partial class PropertiesViewItemCheck : CheckBox
	{

		#region Constants
		internal const string OrientationStateGroupName = "OrientationStateGroup";

		internal const string HorizontalStateName = "HorizontalState";
		internal const string VerticalStateName = "VerticalState";

		#endregion




		#region Properties
		
		public Orientation Orientation
		{
			[DynamicWindowsRuntimeCast( typeof( Orientation ) )]

			get => (Orientation)GetValue( OrientationProperty );
			set => SetValue( OrientationProperty , value );
		}

		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register(
				"Orientation",
				typeof(Orientation),
				typeof(PropertiesViewItemCheck),
				new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

		#endregion




		#region Property Changed

		[DynamicWindowsRuntimeCast( typeof( Orientation ) )]
		private static void OnOrientationChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItemCheck)d;
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

		#endregion




		private bool _isLoaded;
		public PropertiesViewItemCheck()
		{
			DefaultStyleKey = typeof( PropertiesViewItemCheck );
		}




		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateOrientationState( Orientation );
		}
	}
}
