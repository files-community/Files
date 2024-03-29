﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
    public record SelectedTagChangedEventArgs(IEnumerable<(string path, bool isFolder)> Items);
}
