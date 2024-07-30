// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;

namespace Files.App.Controls
{
	public partial class ToolbarButton : ButtonBase
	{
		#region ThemedIcon ( Style )

		public static readonly DependencyProperty ThemedIconProperty =
			DependencyProperty.Register(
				nameof( ThemedIcon ),
				typeof( Style ),
				typeof( ToolbarButton ),
				new PropertyMetadata( null , ( d, e ) => ( ( ToolbarButton ) d ).OnThemedIconPropertyChanged( ( Style )e.OldValue , ( Style )e.NewValue ) ) );


		public Style ThemedIcon
		{
			get { return (Style)GetValue( ThemedIconProperty ); }
			set { SetValue( ThemedIconProperty , value ); }
		}


		protected virtual void OnThemedIconPropertyChanged(Style oldValue , Style newValue)
		{
			if ( newValue != null )
			{
				if ( newValue != oldValue )
				{
					_isThemedIconSet = true;

					ThemedIconChanged( newValue );
				}
			}
			else
			{
				_isThemedIconSet = false;
			}
		}

		#endregion


		#region Label ( string )

		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(
				nameof( Label ),
				typeof( string ),
				typeof( ToolbarButton ),
				new PropertyMetadata( string.Empty , ( d , e ) => ( ( ToolbarButton ) d ).OnLabelPropertyChanged( ( string )e.OldValue , ( string )e.NewValue ) ) );


		public string Label
		{
			get => (string)GetValue( LabelProperty );
			set => SetValue( LabelProperty , value );
		}


		protected virtual void OnLabelPropertyChanged(string oldValue , string newValue)
		{
			LabelChanged( newValue );
		}
		#endregion



		#region LabelLayout ( enum LabelLayouts )

		public static readonly DependencyProperty LabelLayoutProperty =
			DependencyProperty.Register(
				nameof( LabelLayout ),
				typeof( LabelLayouts ),
				typeof( ToolbarButton ),
				new PropertyMetadata( LabelLayouts.Hidden , ( d , e ) => ( ( ToolbarButton ) d ).OnLabelLayoutPropertyChanged( ( LabelLayouts )e.OldValue , ( LabelLayouts )e.NewValue ) ) );


		public LabelLayouts LabelLayout
		{
			get => (LabelLayouts)GetValue( LabelLayoutProperty );
			set => SetValue( LabelLayoutProperty , value );
		}


		protected virtual void OnLabelLayoutPropertyChanged(LabelLayouts oldValue , LabelLayouts newValue)
		{
			LabelLayoutChanged( newValue );
		}

		#endregion
	}
}
