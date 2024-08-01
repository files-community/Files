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
	public partial class ToolbarSplitButton : SplitButton
	{
		public ToolbarSplitButton()
		{
			this.DefaultStyleKey = typeof( ToolbarSplitButton );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}