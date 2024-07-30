// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{

	public partial class ToolbarToggleButton : ToolbarButton
	{
		private bool _isThemedIconSet;
		private bool _isChecked;

		private ThemedIcon ?_themedIcon;



		public ToolbarToggleButton()
		{
			this.DefaultStyleKey = typeof( ToolbarToggleButton );
		}



		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_themedIcon = (ThemedIcon)GetTemplateChild( ButtonThemedIconPartName );
		}



		#region Property Changed Events

		private void IsCheckedChanged( bool newValue )
		{
			// Handles the IsChecked bool property change
			if ( newValue == true )
			{
				_isChecked = true;
			}
			else
			{
				_isChecked = false;
			}
		}

		#endregion


		#region VisualState changes

		private void SetVisualState()
		{
			if ( IsChecked )
			{
				if ( this.IsPointerOver )
				{ }

				if ( this.IsPressed )
				{ }

				if ( this.IsEnabled )
				{ }

				else
				{ }
			}
			else
			{
				if ( this.IsPointerOver )
				{ }

				if ( this.IsPressed )
				{ }

				if ( this.IsEnabled )
				{ }

				else
				{ }
			}

		}

		#endregion


		#region Public Events

		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the IsChecked property is true")]
		public event RoutedEventHandler ?OnToggle;


		private void ToolbarToggleButton_OnToggle( object sender , RoutedEventArgs e )
		{
			OnToggle?.Invoke( this , e );
		}

		#endregion

	}
}
