// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a group or a choice in <see cref="Views.Settings.LayoutPropertyPickerPage"/>.
	/// </summary>
	public sealed partial class LayoutPropertyPickerItem(string displayName, LayoutPropertyPickerItem? group = null, LayoutPropertyChoice? choice = null) : ObservableObject
	{
		public string DisplayName { get; } = displayName;

		/// <summary>
		/// Gets the group of the choice, or null when the item is a group.
		/// </summary>
		public LayoutPropertyPickerItem? Group { get; } = group;

		public LayoutPropertyChoice? Choice { get; } = choice;

		public bool IsGroup
			=> Choice is null;

		public bool IsChoice
			=> !IsGroup;

		public List<LayoutPropertyPickerItem> Choices { get; } = [];

		[ObservableProperty]
		public partial bool IsSelected { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ExpandGlyphAngle))]
		public partial bool IsExpanded { get; set; }

		public double ExpandGlyphAngle
			=> IsExpanded ? 90 : 0;

		public override string ToString()
			=> DisplayName;
	}

	/// <summary>
	/// Chooses the property that items are sorted or grouped by from a grouped list.
	/// </summary>
	public sealed partial class LayoutPropertyPickerViewModel : ObservableObject
	{
		private readonly LayoutPropertyPickerItem[] _groups;
		private LayoutPropertyPickerItem? _selectedItem;
		private readonly LayoutPropertyPickerItem? _firstItem;

		public LayoutPropertyPickerItem? SelectedItem
			=> _selectedItem;

		/// <summary>
		/// Gets the groups and the choices of the expanded groups, in display order.
		/// </summary>
		public ObservableCollection<LayoutPropertyPickerItem> Items { get; } = [];

		private string _SearchQuery = string.Empty;
		public string SearchQuery
		{
			get => _SearchQuery;
			set
			{
				if (SetProperty(ref _SearchQuery, value ?? string.Empty))
					UpdateItems();
			}
		}

		/// <summary>
		/// Occurs when the user confirms the choice.
		/// </summary>
		public event EventHandler<LayoutPropertyChoice>? Selected;

		/// <summary>
		/// Occurs when the window should close, after selecting or canceling.
		/// </summary>
		public event EventHandler? CloseRequested;

		public ICommand SelectCommand { get; }

		public ICommand CancelCommand { get; }

		public string Title { get; }

		public string Description { get; }

		public string SearchPlaceholder { get; }

		/// <param name="firstChoice">The choice listed before the groups, such as not grouping at all, which <paramref name="choices"/> doesn't include.</param>
		public LayoutPropertyPickerViewModel(string title, string description, string searchPlaceholder, IEnumerable<LayoutPropertyChoice> choices, LayoutPropertyChoice? selectedChoice, LayoutPropertyChoice? firstChoice = null)
		{
			Title = title;
			Description = description;
			SearchPlaceholder = searchPlaceholder;

			_groups = choices
				.GroupBy(choice => DetailsLayoutColumnGroups.GetGroup(choice.ColumnId ?? choice.PropertyId))
				.OrderBy(group => group.Key)
				.Select(group =>
				{
					var groupItem = new LayoutPropertyPickerItem(DetailsLayoutColumnGroups.GetDisplayName(group.Key));
					groupItem.Choices.AddRange(group.Select(choice => new LayoutPropertyPickerItem(choice.DisplayName, groupItem, choice)));

					return groupItem;
				}).ToArray();

			SelectCommand = new RelayCommand(Select);
			CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

			if (firstChoice is not null)
				_firstItem = new(firstChoice.DisplayName, choice: firstChoice);

			_selectedItem = firstChoice is not null && ReferenceEquals(firstChoice, selectedChoice)
				? _firstItem
				: _groups.SelectMany(group => group.Choices).FirstOrDefault(item => item.Choice == selectedChoice);
			if (_selectedItem is not null)
			{
				_selectedItem.IsSelected = true;
				if (_selectedItem.Group is { } selectedGroup)
					selectedGroup.IsExpanded = true;
			}

			UpdateItems();
		}

		/// <summary>
		/// Expands or collapses the specified group, or selects the specified choice.
		/// </summary>
		public void Activate(LayoutPropertyPickerItem item)
		{
			if (item.IsGroup)
			{
				ToggleExpanded(item);
				return;
			}

			if (item == _selectedItem)
				return;

			if (_selectedItem is not null)
				_selectedItem.IsSelected = false;

			_selectedItem = item;
			item.IsSelected = true;
		}

		/// <summary>
		/// Expands or collapses the specified group.
		/// </summary>
		public void ToggleExpanded(LayoutPropertyPickerItem group)
		{
			group.IsExpanded = !group.IsExpanded;
			UpdateItems();
		}

		private void Select()
		{
			if (_selectedItem?.Choice is { } choice)
				Selected?.Invoke(this, choice);

			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateItems()
		{
			var query = SearchQuery.Trim();
			var items = new List<LayoutPropertyPickerItem>();
			if (_firstItem is not null)
				items.Add(_firstItem);

			foreach (var group in _groups)
			{
				var choices = group.Choices.Where(choice => query.Length == 0 || choice.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToArray();
				if (choices.Length == 0)
					continue;

				if (query.Length > 0)
					group.IsExpanded = true;

				items.Add(group);
				if (group.IsExpanded)
					items.AddRange(choices);
			}

			var itemSet = items.ToHashSet();
			for (int i = Items.Count - 1; i >= 0; i--)
			{
				if (!itemSet.Contains(Items[i]))
					Items.RemoveAt(i);
			}

			for (int i = 0; i < items.Count; i++)
			{
				if (i >= Items.Count || Items[i] != items[i])
					Items.Insert(i, items[i]);
			}
		}
	}
}
