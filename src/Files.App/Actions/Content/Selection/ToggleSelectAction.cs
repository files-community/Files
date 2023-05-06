// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Actions
{
	internal class ToggleSelectAction : IAction
	{
		public string Label { get; } = "ToggleSelect".GetLocalizedResource();
		public string Description => "ToggleSelectDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Space, KeyModifiers.Ctrl);

		public bool IsExecutable => GetFocusedElement() is not null;

		public Task ExecuteAsync()
		{
			if (GetFocusedElement() is SelectorItem item)
			{
				item.IsSelected = !item.IsSelected;
			}
			return Task.CompletedTask;
		}

		private static SelectorItem? GetFocusedElement()
		{
			return FocusManager.GetFocusedElement(App.Window.Content.XamlRoot) as SelectorItem;
		}
	}
}
