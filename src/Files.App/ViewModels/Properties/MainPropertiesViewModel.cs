using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels;
using Files.App.Views.Properties;
using Files.Backend.Enums;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.ViewModels.Properties
{
	public class MainPropertiesViewModel : ObservableObject
	{
		#region Props
		public CancellationTokenSource ChangedPropertiesCancellationTokenSource { get; }

		public ObservableCollection<NavigationViewItemButtonStyleItem> NavigationViewItems { get; }

		private NavigationViewItemButtonStyleItem _SelectedNavigationViewItem;
		public NavigationViewItemButtonStyleItem SelectedNavigationViewItem
		{
			get => _SelectedNavigationViewItem;
			set
			{
				if (SetProperty(ref _SelectedNavigationViewItem, value) && !SelectionChangedAutomatically)
				{
					var parameter = new PropertiesPageNavigationParameter()
					{
						AppInstance = _parameter.AppInstance,
						CancellationTokenSource = ChangedPropertiesCancellationTokenSource,
						Parameter = _parameter.Parameter,
						Window = Window,
						AppWindow = AppWindow,
					};

					var page = value.ItemType switch
					{
						PropertiesNavigationViewItemType.General =>       typeof(GeneralPage),
						PropertiesNavigationViewItemType.Shortcut =>      typeof(ShortcutPage),
						PropertiesNavigationViewItemType.Library =>       typeof(LibraryPage),
						PropertiesNavigationViewItemType.Details =>       typeof(DetailsPage),
						PropertiesNavigationViewItemType.Security =>      typeof(SecurityPage),
						PropertiesNavigationViewItemType.Customization => typeof(CustomizationPage),
						PropertiesNavigationViewItemType.Compatibility => typeof(CompatibilityPage),
						PropertiesNavigationViewItemType.Hashes =>        typeof(HashesPage),
						_ => typeof(GeneralPage),
					};

					_mainFrame?.Navigate(page, parameter, new EntranceNavigationTransitionInfo());
				}

				SelectionChangedAutomatically = false;
			}
		}

		private readonly Window Window;

		private readonly AppWindow AppWindow;

		private readonly Frame _mainFrame;

		private readonly PropertiesPageNavigationParameter _parameter;

		private bool SelectionChangedAutomatically { get; set; }

		public IRelayCommand DoBackwardNavigationCommand { get; }
		public IAsyncRelayCommand SaveChangedPropertiesCommand { get; }
		public IRelayCommand CancelChangedPropertiesCommand { get; }
		#endregion

		public MainPropertiesViewModel(Window window, AppWindow appWindow, Frame mainFrame, PropertiesPageNavigationParameter parameter)
		{
			ChangedPropertiesCancellationTokenSource = new();

			Window = window;
			AppWindow = appWindow;
			_mainFrame = mainFrame;
			_parameter = parameter;

			DoBackwardNavigationCommand = new RelayCommand(ExecuteDoBackwardNavigationCommand);
			SaveChangedPropertiesCommand = new AsyncRelayCommand(ExecuteSaveChangedPropertiesCommand);
			CancelChangedPropertiesCommand = new RelayCommand(ExecuteCancelChangedPropertiesCommand);

			NavigationViewItems = PropertiesNavigationViewItemFactory.Initialize(parameter.Parameter);
			SelectedNavigationViewItem = NavigationViewItems.Where(x => x.ItemType == PropertiesNavigationViewItemType.General).First();
		}

		private void ExecuteDoBackwardNavigationCommand()
		{
			if (NavigationViewItems is null ||
				NavigationViewItems.Count == 0 ||
				_mainFrame is null)
				return;

			if (_mainFrame.CanGoBack)
				_mainFrame.GoBack();

			var pageTag = ((Page)_mainFrame.Content).Tag.ToString();

			SelectionChangedAutomatically = true;

			// Move selection indicator
			SelectedNavigationViewItem =
				NavigationViewItems.First(x => string.Equals(x.ItemType.ToString(), pageTag, StringComparison.CurrentCultureIgnoreCase))
				?? NavigationViewItems.First();
		}

		private async Task ExecuteSaveChangedPropertiesCommand()
		{
			await ApplyChanges();
			Window.Close();
		}

		private void ExecuteCancelChangedPropertiesCommand()
		{
			Window.Close();
		}

		private async Task ApplyChanges()
		{
			if (_mainFrame is not null && _mainFrame.Content is not null)
				await ((BasePropertiesPage)_mainFrame.Content).SaveChangesAsync();
		}
	}
}
