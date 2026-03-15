// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.UserControls.Menus
{
	public sealed partial class FileTagsContextMenuViewModel : ObservableObject
	{
		private readonly IFileTagsSettingsService _fileTagsSettingsService;

		private readonly IReadOnlyList<ListedItem> _selectedItems;

		public event EventHandler? TagsChanged;

		public IFileTagsSettingsService FileTagsSettingsService
			=> _fileTagsSettingsService;

		public IReadOnlyList<ListedItem> SelectedItems
			=> _selectedItems;

		public IEnumerable<TagViewModel> AvailableTags
			=> FileTagsSettingsService.FileTagList;

		public bool CanRemoveAllTags
			=> SelectedItems.Any(item => item.FileTags is { Length: > 0 });

		public ICommand RemoveAllTagsCommand { get; }

		public FileTagsContextMenuViewModel(IEnumerable<ListedItem> selectedItems, IFileTagsSettingsService fileTagsSettingsService)
		{
			_fileTagsSettingsService = fileTagsSettingsService;
			_selectedItems = selectedItems?.ToList() ?? [];

			RemoveAllTagsCommand = new RelayCommand(ExecuteRemoveAllTagsCommand, CanExecuteRemoveAllTagsCommand);
		}

		public bool IsTagAppliedToAllSelectedItems(TagViewModel tag)
		{
			ArgumentNullException.ThrowIfNull(tag);

			return SelectedItems.Count > 0 &&
				SelectedItems.All(item => (item.FileTags ?? []).Contains(tag.Uid));
		}

		public void UpdateTagSelection(TagViewModel tag, bool isChecked)
		{
			ArgumentNullException.ThrowIfNull(tag);

			var tagsChanged = isChecked
				? AddTagToSelection(tag)
				: RemoveTagFromSelection(tag);

			if (tagsChanged)
				NotifyTagsChanged();
		}

		public bool RemoveAllTags()
		{
			var tagsChanged = false;

			foreach (var selectedItem in SelectedItems)
			{
				if (selectedItem.FileTags is not { Length: > 0 })
					continue;

				selectedItem.FileTags = [];
				tagsChanged = true;
			}

			if (tagsChanged)
				NotifyTagsChanged();

			return tagsChanged;
		}

		private void ExecuteRemoveAllTagsCommand()
			=> RemoveAllTags();

		private bool CanExecuteRemoveAllTagsCommand()
			=> CanRemoveAllTags;

		private bool RemoveTagFromSelection(TagViewModel removed)
		{
			var tagsChanged = false;

			foreach (var selectedItem in SelectedItems)
			{
				var existingTags = selectedItem.FileTags ?? [];
				if (!existingTags.Contains(removed.Uid))
					continue;

				selectedItem.FileTags = existingTags
					.Except([removed.Uid])
					.ToArray();
				tagsChanged = true;
			}

			return tagsChanged;
		}

		private bool AddTagToSelection(TagViewModel added)
		{
			var tagsChanged = false;

			foreach (var selectedItem in SelectedItems)
			{
				var existingTags = selectedItem.FileTags ?? [];
				if (existingTags.Contains(added.Uid))
					continue;

				selectedItem.FileTags = [.. existingTags, added.Uid];
				tagsChanged = true;
			}

			return tagsChanged;
		}

		private void NotifyTagsChanged()
		{
			OnPropertyChanged(nameof(CanRemoveAllTags));
			((RelayCommand)RemoveAllTagsCommand).NotifyCanExecuteChanged();
			TagsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}