﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using System.Collections.Immutable;

namespace Files.App.Data.Contexts
{
    sealed class TagsContext : ITagsContext
    {
		private static readonly IReadOnlyList<FileTagsItemViewModel> _emptyTaggedItemsList
			= Enumerable.Empty<FileTagsItemViewModel>().ToImmutableList();

		public event PropertyChangedEventHandler? PropertyChanged;

		private IEnumerable<FileTagsItemViewModel> _TaggedItems = _emptyTaggedItemsList;
		public IEnumerable<FileTagsItemViewModel> TaggedItems
		{
			get => _TaggedItems;
			set
			{
				_TaggedItems = value is not null
					? value
					: _emptyTaggedItemsList;

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaggedItems)));
			}
		}

		public TagsContext()
		{
			FileTagsContainerViewModel.SelectedTagsChanged += FileTagsContainerViewModel_SelectedTagsChanged;
		}

		private void FileTagsContainerViewModel_SelectedTagsChanged(object sender, IEnumerable<FileTagsItemViewModel> items)
		{
			TaggedItems = items;
		}
	}
}
