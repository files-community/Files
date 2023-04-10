using Files.App.Commands;
using Files.App.Extensions;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class ToggleSelectAction : IAction
	{
		public string Label { get; } = "ToggleSelect".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";

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
