// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using WinRT;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class ToggleSelectAction : IAction
	{
		public string Label
			=> Strings.ToggleSelect.GetLocalizedResource();

		public string Description
			=> Strings.ToggleSelectDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Selection;

		public HotKey HotKey
			=> new(Keys.Space, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> GetFocusedElement() is not null;

		public Task ExecuteAsync(object? parameter = null)
		{
			if (GetFocusedElement() is SelectorItem item)
				item.IsSelected = !item.IsSelected;

			return Task.CompletedTask;
		}

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
		private static SelectorItem? GetFocusedElement()
		{
			try
			{
				return FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot) as SelectorItem;
			}
			catch (COMException) // Window may already be closed
			{
				return null;
			}
		}
	}
}
