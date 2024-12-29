// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
    public record SelectedTagChangedEventArgs(IEnumerable<(string path, bool isFolder)> Items);
}
