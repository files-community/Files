// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Files.App.UITests.Data
{
	internal record BreadcrumbBarItemModel(string Text, ObservableCollection<BreadcrumbBarItemModel>? Children = null);
}
