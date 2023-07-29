// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
    interface ITagsContext: INotifyPropertyChanged
    {
		IEnumerable<(string path, bool isFolder)> TaggedItems { get; }
    }
}
