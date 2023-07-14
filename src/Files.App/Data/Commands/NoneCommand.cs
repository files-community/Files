// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Data.Commands
{
	[DebuggerDisplay("Command None")]
	internal class NoneCommand : IRichCommand
	{
		public event EventHandler? CanExecuteChanged { add { } remove { } }
		public event PropertyChangingEventHandler? PropertyChanging { add { } remove { } }
		public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

		public CommandCodes Code => CommandCodes.None;

		public string Label => string.Empty;
		public string LabelWithHotKey => string.Empty;
		public string AutomationName => string.Empty;

		public string Description => string.Empty;

		public RichGlyph Glyph => RichGlyph.None;
		public object? Icon => null;
		public FontIcon? FontIcon => null;
		public Style? OpacityStyle => null;

		public bool IsCustomHotKeys => false;
		public string? HotKeyText => null;
		public HotKeyCollection HotKeys
		{
			get => HotKeyCollection.Empty;
			set => throw new InvalidOperationException("This command is readonly.");
		}

		public bool IsToggle => false;
		public bool IsOn { get => false; set { } }
		public bool IsExecutable => false;

		public bool CanExecute(object? parameter) => false;
		public void Execute(object? parameter) { }
		public Task ExecuteAsync() => Task.CompletedTask;
		public void ExecuteTapped(object sender, TappedRoutedEventArgs e) { }

		public void ResetHotKeys() { }
	}
}
