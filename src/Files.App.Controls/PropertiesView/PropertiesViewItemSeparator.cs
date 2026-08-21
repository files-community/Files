// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace Files.App.Controls
{

	#region Template
	[TemplateVisualState( Name = HorizontalStateName , GroupName = OrientationStateGroupName )]
	[TemplateVisualState( Name = VerticalStateName , GroupName = OrientationStateGroupName )]
	#endregion

	public sealed partial class PropertiesViewItemSeparator : Control
	{
		#region Constants
		internal const string OrientationStateGroupName = "OrientationStateGroup";

		internal const string HorizontalStateName = "HorizontalState";
		internal const string VerticalStateName = "VerticalState";
		#endregion


		#region Properties

		public Orientation Orientation
		{
			get => (Orientation)GetValue( OrientationProperty );
			set => SetValue( OrientationProperty , value );
		}

		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register(
				"Orientation",
				typeof(Orientation),
				typeof(PropertiesViewItemSeparator),
				new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

		#endregion


		#region Property Changed
		private static void OnOrientationChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItemSeparator)d;
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
		public PropertiesViewItemSeparator()
		{ 
			DefaultStyleKey = typeof( PropertiesViewItemSeparator );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateOrientationState( Orientation );
		}
	}
}
