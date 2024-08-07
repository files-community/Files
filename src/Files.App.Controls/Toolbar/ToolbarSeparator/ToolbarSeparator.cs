using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	public partial class ToolbarSeparator : Control , IToolbarItemSet
	{
		public ToolbarSeparator()
		{
			this.DefaultStyleKey = typeof( ToolbarSeparator );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
