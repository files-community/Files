// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a property that folders can be sorted or grouped by.
	/// </summary>
	/// <param name="ColumnId">The id of the column, used when the property has no built-in option.</param>
	public sealed record LayoutPropertyChoice(string DisplayName, int Option, string ColumnId)
	{
		public override string ToString()
			=> DisplayName;
	}

	/// <summary>
	/// Lets the user find a property to sort or group by in a long list by typing part of its name.
	/// </summary>
	public sealed partial class LayoutPropertyPickerViewModel : ObservableObject
	{
		private const int MaxSuggestions = 100;

		private readonly IReadOnlyList<LayoutPropertyChoice> _choices;
		private readonly Action<LayoutPropertyChoice> _select;
		private LayoutPropertyChoice? _selectedChoice;

		public ObservableCollection<LayoutPropertyChoice> Suggestions { get; } = [];

		private string _Text = string.Empty;
		public string Text
		{
			get => _Text;
			set => SetProperty(ref _Text, value);
		}

		public LayoutPropertyPickerViewModel(IReadOnlyList<LayoutPropertyChoice> choices, LayoutPropertyChoice? selectedChoice, Action<LayoutPropertyChoice> select)
		{
			_choices = choices;
			_select = select;
			_selectedChoice = selectedChoice;
			_Text = selectedChoice?.DisplayName ?? string.Empty;
		}

		/// <summary>
		/// Shows the properties whose name contains the specified text.
		/// </summary>
		public void Filter(string? query)
		{
			Suggestions.Clear();
			foreach (var choice in _choices.Where(choice => string.IsNullOrWhiteSpace(query) || choice.DisplayName.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase)).Take(MaxSuggestions))
				Suggestions.Add(choice);
		}

		public void Choose(LayoutPropertyChoice choice)
		{
			_selectedChoice = choice;
			Text = choice.DisplayName;
			_select(choice);
		}
	}
}
