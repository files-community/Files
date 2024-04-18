// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Actions;
using Microsoft.AppCenter.Analytics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Data.Commands
{
	[DebuggerDisplay("Command {Code}")]
	internal sealed class ActionCommand : ObservableObject, IRichCommand
	{
		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();

		public event EventHandler? CanExecuteChanged;

		public IAction Action { get; }

		public CommandCodes Code { get; }

		public string Label
			=> Action.Label;

		public string LabelWithHotKey
			=> HotKeyText is null ? Label : $"{Label} ({HotKeyText})";

		public string AutomationName
			=> Label;

		public string Description
			=> Action.Description;

		public RichGlyph Glyph
			=> Action.Glyph;

		public object? Icon { get; }

		public FontIcon? FontIcon { get; }

		public Style? OpacityStyle { get; }

		private bool isCustomHotKeys = false;
		public bool IsCustomHotKeys
		{
			get => isCustomHotKeys;
			set => SetProperty(ref isCustomHotKeys, value);
		}

		public string? HotKeyText
		{
			get
			{
				string text = HotKeys.LocalizedLabel;
				if (string.IsNullOrEmpty(text))
					return null;
				return text;
			}
		}

		private HotKeyCollection hotKeys;
		public HotKeyCollection HotKeys
		{
			get => hotKeys;
			set
			{
				if (SetProperty(ref hotKeys, value))
				{
					OnPropertyChanged(nameof(HotKeyText));
					OnPropertyChanged(nameof(LabelWithHotKey));
					IsCustomHotKeys = true;
				}
			}
		}

		public HotKeyCollection DefaultHotKeys { get; }

		public bool IsToggle
			=> Action is IToggleAction;

		public bool IsOn
		{
			get => Action is IToggleAction toggleAction && toggleAction.IsOn;
			set
			{
				if (Action is IToggleAction toggleAction && toggleAction.IsOn != value)
					Execute(null);
			}
		}

		public bool IsExecutable
			=> Action.IsExecutable;

		public ActionCommand(CommandManager manager, CommandCodes code, IAction action)
		{
			Code = code;
			Action = action;
			Icon = action.Glyph.ToIcon();
			FontIcon = action.Glyph.ToFontIcon();
			OpacityStyle = action.Glyph.ToOpacityStyle();
			hotKeys = CommandManager.GetDefaultKeyBindings(action);
			DefaultHotKeys = CommandManager.GetDefaultKeyBindings(action);

			if (action is INotifyPropertyChanging notifyPropertyChanging)
				notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
			if (action is INotifyPropertyChanged notifyPropertyChanged)
				notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
		}

		public bool CanExecute(object? parameter)
		{
			return Action.IsExecutable;
		}

		public async void Execute(object? parameter)
		{
			await ExecuteAsync();
		}

		public Task ExecuteAsync()
		{
			if (IsExecutable)
			{
				Analytics.TrackEvent($"Triggered {Code} action");
				return Action.ExecuteAsync();
			}

			return Task.CompletedTask;
		}

		public async void ExecuteTapped(object sender, TappedRoutedEventArgs e)
		{
			await ExecuteAsync();
		}

		internal void OverwriteKeyBindings(HotKeyCollection hotKeys)
		{
			HotKeys = hotKeys;
		}

		internal void RestoreKeyBindings()
		{
			hotKeys = DefaultHotKeys;
			IsCustomHotKeys = false;
		}

		private void Action_PropertyChanging(object? sender, PropertyChangingEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanging(nameof(Label));
					OnPropertyChanging(nameof(LabelWithHotKey));
					OnPropertyChanging(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanging(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanging(nameof(IsExecutable));
					break;
			}
		}

		private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanged(nameof(Label));
					OnPropertyChanged(nameof(LabelWithHotKey));
					OnPropertyChanged(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanged(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanged(nameof(IsExecutable));
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					break;
			}
		}
	}
}
