// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Views.Settings;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a layout template edited in Settings.
	/// </summary>
	public sealed partial class LayoutTemplateViewModel : ObservableObject
	{
		// The combo boxes list these options in this order
		private static readonly SortOption[] SortOptions = [SortOption.Name, SortOption.DateModified, SortOption.DateCreated, SortOption.Size, SortOption.FileType, SortOption.FileTag];
		private static readonly GroupOption[] GroupOptions = [GroupOption.None, GroupOption.Name, GroupOption.DateModified, GroupOption.DateCreated, GroupOption.Size, GroupOption.FileType, GroupOption.FileTag];

		private readonly ILayoutTemplatesService _templatesService;

		public string Id { get; }

		public LayoutTemplate Template
			=> _templatesService.GetTemplate(Id) ?? _templatesService.DefaultTemplate;

		public string DisplayName
			=> Template.DisplayName;

		public int SelectedLayoutModeIndex
		{
			get => (int)Template.LayoutMode;
			set => Update(template => template.LayoutMode = (FolderLayoutModes)value, value >= 0);
		}

		public int SelectedSortingIndex
		{
			get => Array.IndexOf(SortOptions, Template.SortOption);
			set => Update(template => template.SortOption = SortOptions[value], value >= 0);
		}

		public bool SortInDescendingOrder
		{
			get => Template.SortDirection is SortDirection.Descending;
			set => Update(template => template.SortDirection = value ? SortDirection.Descending : SortDirection.Ascending);
		}

		public int SelectedSortPriorityIndex
		{
			get => Template.SortDirectoriesAlongsideFiles ? 2 : Template.SortFilesFirst ? 1 : 0;
			set => Update(template =>
			{
				template.SortDirectoriesAlongsideFiles = value is 2;
				template.SortFilesFirst = value is 1;
			}, value >= 0);
		}

		public int SelectedGroupingIndex
		{
			get => Array.IndexOf(GroupOptions, Template.GroupOption);
			set => Update(template => template.GroupOption = GroupOptions[value], value >= 0);
		}

		public bool GroupInDescendingOrder
		{
			get => Template.GroupDirection is SortDirection.Descending;
			set => Update(template => template.GroupDirection = value ? SortDirection.Descending : SortDirection.Ascending);
		}

		public int SelectedGroupByDateUnitIndex
		{
			get => (int)Template.GroupByDateUnit;
			set => Update(template => template.GroupByDateUnit = (GroupByDateUnit)value, value >= 0);
		}

		public bool IsGrouped
			=> Template.GroupOption is not GroupOption.None;

		public bool IsGroupByDate
			=> Template.GroupOption.IsGroupByDate();

		public ICommand CustomizeColumnsCommand { get; }

		public LayoutTemplateViewModel(ILayoutTemplatesService templatesService, string id)
		{
			_templatesService = templatesService;
			Id = id;

			CustomizeColumnsCommand = new AsyncRelayCommand(CustomizeColumnsAsync);
		}

		/// <summary>
		/// Raises change notifications after the template changed elsewhere.
		/// </summary>
		public void Refresh()
		{
			OnPropertyChanged(string.Empty);
		}

		private void Update(Action<LayoutTemplate> update, bool isValid = true)
		{
			if (!isValid)
				return;

			var template = Template.Clone();
			update(template);
			_templatesService.SaveTemplate(template);
			Refresh();
		}

		private async Task CustomizeColumnsAsync()
		{
			var catalog = await ShellPropertyCatalog.GetAsync();
			var availableColumns = DetailsLayoutColumnIds.All
				.Where(id => id != DetailsLayoutColumnIds.Icon)
				.Concat(catalog.Keys.Where(id => !DetailsLayoutColumnDefinitions.IsBuiltIn(id)));

			var template = Template;
			var viewModel = new ColumnCustomizationViewModel(
				DetailsLayoutColumnDefaults.GetColumns(template),
				availableColumns,
				DetailsLayoutColumnDefaults.GetOriginalColumns(template),
				id => DetailsLayoutColumnDefaults.GetDisplayName(id, catalog),
				string.Format(Strings.CustomizeTemplateColumns.GetLocalizedResource(), template.DisplayName),
				Strings.TemplateColumnsDescription.GetLocalizedResource());

			viewModel.Saved += (_, columns) => Update(template => DetailsLayoutColumnDefaults.SetColumns(template, columns));
			ColumnCustomizationWindow.Show(viewModel);
		}
	}
}
