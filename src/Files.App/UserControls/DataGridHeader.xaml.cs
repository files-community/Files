// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Files.App.UserControls
{
	public sealed partial class DataGridHeader : UserControl, INotifyPropertyChanged
	{
		// Properties

		public ICommand? Command { get; set; }
		public object? CommandParameter { get; set; }

		private string? _Header;
		public string? Header
		{
			get { return _Header; }
			set
			{
				if (value != _Header)
				{
					_Header = value;
					NotifyPropertyChanged(nameof(Header));
				}
			}
		}

		private bool _CanSort = true;
		public bool CanSort
		{
			get { return _CanSort; }
			set
			{
				if (value != _CanSort)
				{
					_CanSort = value;
					NotifyPropertyChanged(nameof(CanSort));
				}
			}
		}

		private SortDirection? _SortDirection;
		public SortDirection? SortDirection
		{
			get => _SortDirection;
			set
			{
				if (value != _SortDirection)
				{
					_SortDirection = value;

					var direction = _SortDirection switch
					{
						Core.Data.Enums.SortDirection.Ascending => "SortAscending",
						Core.Data.Enums.SortDirection.Descending => "SortDescending",
						_ => "Unsorted",
					};

					VisualStateManager.GoToState(this, direction, true);
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
