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

	public partial class ToolbarButton : ButtonBase
	{
		private bool _isThemedIconSet;

		private ThemedIcon ?_themedIcon;



		public ToolbarButton()
		{
			this.DefaultStyleKey = typeof( ToolbarButton );
		}



		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_themedIcon = (ThemedIcon)GetTemplateChild( ButtonThemedIconPartName );
		}



		#region Property Changed Events

		private void ThemedIconChanged(Style newStyle)
		{
			// Handles changes to the ThemedIcon part's Style

			if ( _themedIcon != null )
			{
				_isThemedIconSet = true;
			}
			else
			{
				_isThemedIconSet = false;
			}
		}



		private void LabelChanged(string newLabel)
		{
			// Handles changes to the Label string property
		}



		private void LabelLayoutChanged(LabelLayouts newLabelLayout)
		{
			// Handles changes to the LabelLayout enum property
		}

		#endregion


		#region VisualState changes

		private void SetVisualState()
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

		#endregion


		#region Public Events



		#endregion

	}
}
