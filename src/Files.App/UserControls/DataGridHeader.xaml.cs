// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.UserControls
{
	public sealed partial class DataGridHeader : UserControl
	{
		[GeneratedDependencyProperty]
		public partial string Header { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanBeSorted { get; set; }

		[GeneratedDependencyProperty]
		public partial SortDirection? ColumnSortOption { get; set; }

		public ICommand Command { get; set; }
		public object CommandParameter { get; set; }

		partial void OnColumnSortOptionChanged(SortDirection? newValue)
		{
			VisualStateManager.GoToState(
				this,
				newValue switch
				{
					SortDirection.Ascending => "SortAscending",
					SortDirection.Descending => "SortDescending",
					_ => "Unsorted",
				},
				true);
		}

		public DataGridHeader()
		{
			InitializeComponent();
		}
	}
}
