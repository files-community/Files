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
	public partial class ToolbarToggleButton : ToolbarButton
	{
		#region IsChecked ( bool )

		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(
				nameof( IsChecked ),
				typeof( bool ),
				typeof( ThemedIcon ),
				new PropertyMetadata( string.Empty , ( d , e ) => ( ( ToolbarToggleButton ) d ).OnIsCheckedPropertyChanged( ( bool )e.OldValue , ( bool )e.NewValue ) ) );


		public bool IsChecked
		{
			get => (bool)GetValue( IsCheckedProperty );
			set => SetValue( IsCheckedProperty , value );
		}


		protected virtual void OnIsCheckedPropertyChanged(bool oldValue , bool newValue)
		{
			IsCheckedChanged( newValue );
		}

		#endregion
	}
}
