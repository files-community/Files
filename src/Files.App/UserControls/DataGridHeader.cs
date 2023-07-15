// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.UserControls
{
	public class DataGridHeader : ButtonBase
	{
		internal const string NormalState = "Normal";
		internal const string SortAscendingState = "SortAscending";
		internal const string SortDescendingState = "SortDescending";

		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register(
				nameof(Header),
				typeof(string),
				typeof(DataGridHeader),
				new PropertyMetadata(null));

		public string Header
		{
			get => (string)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static readonly DependencyProperty ColumnSortOptionProperty =
			DependencyProperty.Register(
				nameof(ColumnSortOption),
				typeof(SortDirection?),
				typeof(DataGridHeader),
				new PropertyMetadata(null, (d, e) => ((DataGridHeader)d).OnColumnSortOptionPropertyChanged()));

		public SortDirection? ColumnSortOption
		{
			get => (SortDirection?)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public DataGridHeader()
		{
		}

		private void OnColumnSortOptionPropertyChanged()
		{
			_ = ColumnSortOption switch
			{
				SortDirection.Ascending => VisualStateManager.GoToState(this, SortAscendingState, true),
				SortDirection.Descending => VisualStateManager.GoToState(this, SortDescendingState, true),
				_ => VisualStateManager.GoToState(this, NormalState, true),
			};
		}
	}
}
