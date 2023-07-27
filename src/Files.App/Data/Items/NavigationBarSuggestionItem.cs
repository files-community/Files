// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Items
{
	public class NavigationBarSuggestionItem : ObservableObject
	{
		private string? _Text;
		public string? Text
		{
			get => _Text;
			set => SetProperty(ref _Text, value);
		}

		private string? _PrimaryDisplay;
		public string? PrimaryDisplay
		{
			get => _PrimaryDisplay;
			set => SetProperty(ref _PrimaryDisplay, value);
		}

		private string? _SecondaryDisplay;
		public string? SecondaryDisplay
		{
			get => _SecondaryDisplay;
			set => SetProperty(ref _SecondaryDisplay, value);
		}

		private string? _SupplementaryDisplay;
		public string? SupplementaryDisplay
		{
			get => _SupplementaryDisplay;
			set => SetProperty(ref _SupplementaryDisplay, value);
		}

		private Brush _DisplayForeground = App.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
		public Brush DisplayForeground
		{
			get => _DisplayForeground;
			set => SetProperty(ref _DisplayForeground, value);
		}
	}
}
