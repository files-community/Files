// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Files.App.UserControls
{
	// TODO: Rebase this class with ButtonBase instead of UserControl
	public sealed partial class DataGridHeader : UserControl, INotifyPropertyChanged
	{
		// Properties

		public ICommand Command { get; set; }
		public object CommandParameter { get; set; }

		private string _Header = string.Empty;
		public string Header
		{
			get => _Header;
			set
			{
				if (value != _Header)
				{
					_Header = value;
					NotifyPropertyChanged(nameof(Header));
				}
			}
		}

		private bool _CanBeSorted = true;
		public bool CanBeSorted
		{
			get { return _CanBeSorted; }
			set
			{
				if (value != _CanBeSorted)
				{
					_CanBeSorted = value;
					NotifyPropertyChanged(nameof(CanBeSorted));
				}
			}
		}

		private SortDirection? _ColumnSortOption;
		public SortDirection? ColumnSortOption
		{
			get { return _ColumnSortOption; }
			set
			{
				if (value != _ColumnSortOption)
				{
					switch (value)
					{
						case SortDirection.Ascending:
							VisualStateManager.GoToState(this, "SortAscending", true);
							break;
						case SortDirection.Descending:
							VisualStateManager.GoToState(this, "SortDescending", true);
							break;
						default:
							VisualStateManager.GoToState(this, "Unsorted", true);
							break;
					}

					_ColumnSortOption = value;
				}
			}
		}

		// Events

		public event PropertyChangedEventHandler? PropertyChanged;

		// Constructor

		public DataGridHeader()
		{
			InitializeComponent();
		}

		// Event methods

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}