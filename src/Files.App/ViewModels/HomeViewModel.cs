// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls.Widgets;
using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public class HomeViewModel : ObservableObject, IDisposable
	{
		private readonly WidgetsListControlViewModel widgetsViewModel;

		private IShellPage associatedInstance;

		public event EventHandler<RoutedEventArgs> YourHomeLoadedInvoked;

		public ICommand YourHomeLoadedCommand { get; private set; }

		public HomeViewModel(WidgetsListControlViewModel widgetsViewModel, IShellPage associatedInstance)
		{
			this.widgetsViewModel = widgetsViewModel;
			this.associatedInstance = associatedInstance;

			// Create commands
			YourHomeLoadedCommand = new RelayCommand<RoutedEventArgs>(YourHomeLoaded);
		}

		public void ChangeAppInstance(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;
		}

		private void YourHomeLoaded(RoutedEventArgs e)
		{
			YourHomeLoadedInvoked?.Invoke(this, e);
		}

		public void Dispose()
		{
			widgetsViewModel?.Dispose();
		}
	}
}
