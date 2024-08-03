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
using System.ComponentModel;

namespace Files.App.Controls
{
	public partial class ToolbarButton : Button
	{

		#region Label (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Label"/> property.
		/// </summary>
		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(
				nameof(Label),
				typeof(string),
				typeof(ToolbarButton),
				new PropertyMetadata(string.Empty, (d, e) => ((ToolbarButton)d).OnLabelPropertyChanged((string)e.OldValue, (string)e.NewValue)));



		/// <summary>
		/// Gets or sets the Label as a String
		/// </summary>
		public string Label
		{
			get => (string)GetValue( LabelProperty );
			set => SetValue( LabelProperty , value );
		}



		protected virtual void OnLabelPropertyChanged(string oldValue , string newValue)
		{
			if ( oldValue != newValue )
			{
				LabelChanged( newValue );
			}
		}

		#endregion



		#region ThemedIcon (style)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ThemedIcon"/> property.
		/// </summary>
		public static readonly DependencyProperty ThemedIconProperty =
			DependencyProperty.Register(
				nameof(ThemedIcon),
				typeof(Style),
				typeof(ToolbarButton),
				new PropertyMetadata(null, (d, e) => ((ToolbarButton)d).OnThemedIconPropertyChanged((Style)e.OldValue, (Style)e.NewValue)));



		/// <summary>
		/// Gets or sets the Style value for the item's ThemedIcon
		/// </summary>
		public Style ThemedIcon
		{
			get => (Style)GetValue( ThemedIconProperty );
			set => SetValue( ThemedIconProperty , value );
		}



		protected virtual void OnThemedIconPropertyChanged(Style oldValue , Style newValue)
		{
			if ( newValue != oldValue )
			{
				ThemedIconChanged( newValue );
			}
		}

		#endregion



		#region ButtonBase Events

		/// <inheritdoc/>
		protected override void OnContentChanged(object oldContent , object newContent)
		{
			if ( newContent != oldContent )
			{
				ContentChanged( newContent );
				base.OnContentChanged( oldContent , newContent );
			}
		}

		#endregion

	}
}
