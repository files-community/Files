// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Files.App.Views.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.ViewModels.Properties
{
	public sealed partial class MainPropertiesViewModel : ObservableObject
	{
		public CancellationTokenSource ChangedPropertiesCancellationTokenSource { get; }

		public ObservableCollection<PropertiesNavigationItem> NavigationItems { get; }

		public ObservableCollection<FlatSidebarItem> FlatNavigationItems { get; } = [];

		private PropertiesNavigationItem _SelectedNavigationItem;
		public PropertiesNavigationItem SelectedNavigationItem
		{
			get => _SelectedNavigationItem;
			set
			{
				if (SetProperty(ref _SelectedNavigationItem, value))
					NavigateToPage(value);
			}
		}

		private readonly Window Window;

		private AppWindow AppWindow => Window.AppWindow;
		private readonly Frame _mainFrame;

		private readonly BaseProperties _baseProperties;

		private readonly PropertiesPageNavigationParameter _parameter;

		public IRelayCommand DoBackwardNavigationCommand { get; }
		public IAsyncRelayCommand SaveChangedPropertiesCommand { get; }
		public IRelayCommand CancelChangedPropertiesCommand { get; }

		public MainPropertiesViewModel(Window window, Frame mainFrame, BaseProperties baseProperties, PropertiesPageNavigationParameter parameter)
		{
			ChangedPropertiesCancellationTokenSource = new();

			Window = window;
			_mainFrame = mainFrame;
			_parameter = parameter;
			_baseProperties = baseProperties;

			DoBackwardNavigationCommand = new RelayCommand(ExecuteDoBackwardNavigationCommand);
			SaveChangedPropertiesCommand = new AsyncRelayCommand(ExecuteSaveChangedPropertiesCommandAsync);
			CancelChangedPropertiesCommand = new RelayCommand(ExecuteCancelChangedPropertiesCommand);

			NavigationItems = PropertiesNavigationItemsFactory.Initialize(parameter.Parameter);
			foreach (var navItem in NavigationItems)
				FlatNavigationItems.Add(new FlatSidebarItem(navItem, 0));

			SelectedNavigationItem = NavigationItems.First(x => x.ItemType == PropertiesNavigationViewItemType.General);
		}

		private void NavigateToPage(PropertiesNavigationItem item)
		{
			foreach (var navItem in NavigationItems)
			{
				navItem.IconElement.IsFilled = navItem == item;
				navItem.IconElement.IconType = ThemedIconTypes.Outline;
			}

			var parameter = new PropertiesPageNavigationParameter
			{
				AppInstance = _parameter.AppInstance,
				CancellationTokenSource = ChangedPropertiesCancellationTokenSource,
				Parameter = _parameter.Parameter,
				Window = Window
			};

			var page = item.ItemType switch
			{
				PropertiesNavigationViewItemType.General => typeof(GeneralPage),
				PropertiesNavigationViewItemType.Shortcut => typeof(ShortcutPage),
				PropertiesNavigationViewItemType.Library => typeof(LibraryPage),
				PropertiesNavigationViewItemType.Details => typeof(DetailsPage),
				PropertiesNavigationViewItemType.Security => typeof(SecurityPage),
				PropertiesNavigationViewItemType.Customization => typeof(CustomizationPage),
				PropertiesNavigationViewItemType.Compatibility => typeof(CompatibilityPage),
				PropertiesNavigationViewItemType.Hashes => typeof(HashesPage),
				PropertiesNavigationViewItemType.Signatures => typeof(SignaturesPage),
				_ => typeof(GeneralPage),
			};

			_mainFrame?.Navigate(page, parameter, new EntranceNavigationTransitionInfo());
		}

		private void ExecuteDoBackwardNavigationCommand()
		{
			if (NavigationItems is null ||
				NavigationItems.Count == 0 ||
				_mainFrame is null)
				return;

			if (_mainFrame.CanGoBack)
				_mainFrame.GoBack();

			var pageTag = ((Page)_mainFrame.Content).Tag.ToString();

			_SelectedNavigationItem =
				NavigationItems.First(x => string.Equals(x.ItemType.ToString(), pageTag, StringComparison.CurrentCultureIgnoreCase))
				?? NavigationItems.First();

			foreach (var item in NavigationItems)
			{
				item.IconElement.IsFilled = item == _SelectedNavigationItem;
				item.IconElement.IconType = ThemedIconTypes.Outline;
			}

			OnPropertyChanged(nameof(SelectedNavigationItem));
		}

		private async Task ExecuteSaveChangedPropertiesCommandAsync()
		{
			await ApplyChangesAsync();
			Window.Close();
		}

		private void ExecuteCancelChangedPropertiesCommand()
		{
			Window.Close();
		}

		private async Task ApplyChangesAsync()
		{
			if (_mainFrame is not null && _mainFrame.Content is not null)
				await ((BasePropertiesPage)_mainFrame.Content).SaveChangesAsync();
		}
	}

	public sealed partial class PropertiesNavigationItem : ObservableObject, ISidebarItemModel
	{
		public PropertiesNavigationViewItemType ItemType { get; }
		public string Text { get; }
		public ThemedIcon IconElement { get; }

		public object? Children => null;
		public string? Path => null;
		[ObservableProperty] public partial bool IsExpanded { get; set; }

		public object? ToolTip => Text;
		public object? ItemDecorator => null;

		public PropertiesNavigationItem(PropertiesNavigationViewItemType itemType, string text, ThemedIcon iconElement)
		{
			ItemType = itemType;
			Text = text;
			IconElement = iconElement;
		}
	}
}
