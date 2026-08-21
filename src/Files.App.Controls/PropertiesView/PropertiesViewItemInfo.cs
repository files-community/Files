// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Controls.Primitives;
using Microsoft.UI;
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

	#region Template Parts

	[TemplatePart( Name = InfoCardPartName , Type = typeof( Border ) )]


	[TemplateVisualState( Name = InfoRestStateName , GroupName = InfoStateGroupName )]
	[TemplateVisualState( Name = InfoPointerOverStateName , GroupName = InfoStateGroupName )]

	#endregion

	public sealed partial class PropertiesViewItemInfo : PropertiesViewItem
	{
		#region Constants
		internal const string InfoCardPartName = "PART_InfoCard";

		internal const string InfoStateGroupName = "InfoStateGroup";

		internal const string InfoRestStateName = "InfoRest";
		internal const string InfoPointerOverStateName = "InfoPointerOver";

		#endregion




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
				typeof(PropertiesViewItemInfo),
				new PropertyMetadata( string.Empty ) );




		public string InfoText
		{
			get { return (string)GetValue( InfoTextProperty ); }
			set { SetValue( InfoTextProperty , value ); }
		}

		public static readonly DependencyProperty InfoTextProperty =
			DependencyProperty.Register(
				"InfoText",
				typeof(string),
				typeof(PropertiesViewItemInfo),
				new PropertyMetadata( string.Empty ) );

		#endregion


		public PropertiesViewItemInfo()
		{
			DefaultStyleKey = typeof( PropertiesViewItemInfo );
		}




		private Border? _hoverBorder;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_hoverBorder = GetTemplateChild( InfoCardPartName ) as Border;

			if ( _hoverBorder != null )
			{
				_hoverBorder.PointerEntered += OnHoverBorderPointerEntered;
				_hoverBorder.PointerExited += OnHoverBorderPointerExited;
			}
		}




		private void OnHoverBorderPointerEntered(object sender , PointerRoutedEventArgs e)
		{
			if (IsEnabled)
				VisualStateManager.GoToState( this , InfoPointerOverStateName , true );
		}

		private void OnHoverBorderPointerExited(object sender , PointerRoutedEventArgs e)
		{
			if ( IsEnabled )
				VisualStateManager.GoToState( this , InfoRestStateName , true );
		}
	}
}
