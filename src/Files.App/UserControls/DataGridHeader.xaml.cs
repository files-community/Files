// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

// Il modello di elemento Controllo utente è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class DataGridHeader : UserControl, INotifyPropertyChanged
	{
		public ICommand Command { get; set; }
		public object CommandParameter { get; set; }

		private string header;

		public string Header
		{
			get { return header; }
			set
			{
				if (value != header)
				{
					header = value;
					NotifyPropertyChanged(nameof(Header));
				}
			}
		}

		private bool canBeSorted = true;

		public bool CanBeSorted
		{
			get { return canBeSorted; }
			set
			{
				if (value != canBeSorted)
				{
					canBeSorted = value;
					NotifyPropertyChanged(nameof(CanBeSorted));
				}
			}
		}

		private SortDirection? columnSortOption;

		public SortDirection? ColumnSortOption
		{
			get { return columnSortOption; }
			set
			{
				if (value != columnSortOption)
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
					columnSortOption = value;
				}
			}
		}

		public DataGridHeader()
		{
			InitializeComponent();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}