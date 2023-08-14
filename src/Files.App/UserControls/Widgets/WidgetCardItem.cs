// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.UserControls.Widgets
{
	public abstract class WidgetCardItem : ObservableObject
	{
		public virtual string Path { get; set; }

		public virtual object Item { get; set; }
	}
}
