// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Controls
{
	public partial class TableViewCheckBoxColumn
	{
		[GeneratedDependencyProperty]
		public partial string? IsEnabledBinding { get; set; }

		[GeneratedDependencyProperty]
		public partial string? VisibilityBinding { get; set; }

		[GeneratedDependencyProperty]
		public partial IValueConverter? VisibilityConverter { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsThreeState { get; set; }

		partial void OnIsEnabledBindingChanged(string? newValue)
		{
			NotifyPropertyChanged(TableViewNotificationTarget.VisibleRows);
		}

		partial void OnVisibilityBindingChanged(string? newValue)
		{
			NotifyPropertyChanged(TableViewNotificationTarget.VisibleRows);
		}

		partial void OnVisibilityConverterChanged(IValueConverter? newValue)
		{
			NotifyPropertyChanged(TableViewNotificationTarget.VisibleRows);
		}

		partial void OnIsThreeStateChanged(bool newValue)
		{
			NotifyPropertyChanged(TableViewNotificationTarget.VisibleRows);
		}

	}
}
