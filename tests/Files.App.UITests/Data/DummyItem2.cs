// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Files.App.UITests.Data
{
	internal record DummyItem2(string Text, ObservableCollection<DummyItem2>? Children = null);
}
