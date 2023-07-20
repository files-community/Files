// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;

namespace Files.App.Data.Contexts
{
    interface ITagsContext: INotifyPropertyChanged
    {
		IEnumerable<FileTagsItemViewModel> TaggedItems { get; }
    }
}
