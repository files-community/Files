// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
			set
			{
				if (SetProperty(ref _PrimaryDisplay, value))
					UpdatePrimaryDisplay();
			}
		}

		private string? _SearchText;
		public string? SearchText
		{
			get => _SearchText;
			set
			{
				if (SetProperty(ref _SearchText, value))
					UpdatePrimaryDisplay();
			}
		}

		private string? _PrimaryDisplayPreMatched;
		public string? PrimaryDisplayPreMatched
		{
			get => _PrimaryDisplayPreMatched;
			private set => SetProperty(ref _PrimaryDisplayPreMatched, value);
		}

		private string? _PrimaryDisplayMatched;
		public string? PrimaryDisplayMatched
		{
			get => _PrimaryDisplayMatched;
			private set => SetProperty(ref _PrimaryDisplayMatched, value);
		}

		private string? _PrimaryDisplayPostMatched;
		public string? PrimaryDisplayPostMatched
		{
			get => _PrimaryDisplayPostMatched;
			private set => SetProperty(ref _PrimaryDisplayPostMatched, value);
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

		private void UpdatePrimaryDisplay()
		{
			if (SearchText is null || PrimaryDisplay is null)
			{
				PrimaryDisplayPreMatched = null;
				PrimaryDisplayMatched = PrimaryDisplay;
				PrimaryDisplayPostMatched = null;
			}
			else
			{
				var index = PrimaryDisplay.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase);
				if (index < 0)
				{
					PrimaryDisplayPreMatched = PrimaryDisplay;
					PrimaryDisplayMatched = null;
					PrimaryDisplayPostMatched = null;
				}
				else
				{
					PrimaryDisplayPreMatched = PrimaryDisplay.Substring(0, index);
					PrimaryDisplayMatched = PrimaryDisplay.Substring(index, SearchText.Length);
					PrimaryDisplayPostMatched = PrimaryDisplay.Substring(index + SearchText.Length);
				}
			}
		}
	}
}
