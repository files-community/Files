using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Enums;
using Microsoft.UI.Xaml;

namespace Files.App.DataModels
{
	public class NavigationViewItemButtonStyleItem : ObservableObject
	{
		public string? Name;

		public PropertiesNavigationViewItemType ItemType;

		private Style _OpacityIconStyle = (Style)Application.Current.Resources["ColorIconGeneralProperties"];
		public Style OpacityIconStyle
		{
			get => _OpacityIconStyle;
			set => SetProperty(ref _OpacityIconStyle, value);
		}

		private bool _IsSelected;
		public bool IsSelected
		{
			get => _IsSelected;
			set => SetProperty(ref _IsSelected, value);
		}

		private bool _IsCompact;
		public bool IsCompact
		{
			get => _IsCompact;
			set => SetProperty(ref _IsCompact, value);
		}
	}
}
