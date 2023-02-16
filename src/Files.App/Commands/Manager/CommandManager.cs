using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Actions;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.Commands
{
	internal class CommandManager : ICommandManager
	{
		private readonly IDictionary<CommandCodes, IRichCommand> commands = new Dictionary<CommandCodes, IRichCommand>
		{
			[CommandCodes.None] = new NoneCommand(),
		};

		public IRichCommand this[CommandCodes code] => GetCommand(code);

		public IRichCommand None => GetCommand(CommandCodes.None);
		public IRichCommand ShowHiddenItems => GetCommand(CommandCodes.ShowHiddenItems);
		public IRichCommand ShowFileExtensions => GetCommand(CommandCodes.ShowFileExtensions);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => commands.Values.GetEnumerator();

		private IRichCommand GetCommand(CommandCodes code)
		{
			if (commands.TryGetValue(code, out IRichCommand? command))
				return command;

			var action = GetAction(code);
			var newCommand = new ActionCommand(code, action);
			commands.Add(code, newCommand);

			return newCommand;
		}

		private static IAction GetAction(CommandCodes code) => code switch
		{
			CommandCodes.ShowHiddenItems => new ShowHiddenItemsAction(),
			CommandCodes.ShowFileExtensions => new ShowFileExtensionsAction(),
			_ => throw new ArgumentOutOfRangeException(nameof(code)),
		};

		[DebuggerDisplay("Command None")]
		private class NoneCommand : IRichCommand
		{
			public event EventHandler? CanExecuteChanged { add {} remove {} }
			public event PropertyChangingEventHandler? PropertyChanging { add { } remove { } }
			public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

			public CommandCodes Code => CommandCodes.None;

			public string Label => string.Empty;

			public bool IsToggle => false;
			public bool IsOn { get => false; set {} }
			public bool IsExecutable => false;

			public bool CanExecute(object? _) => false;
			public void Execute(object? _) {}
			public Task ExecuteAsync() => Task.CompletedTask;
			public void ExecuteTapped(object _, TappedRoutedEventArgs e) {}
		}

		[DebuggerDisplay("Command {Code}")]
		private class ActionCommand : ObservableObject, IRichCommand
		{
			public event EventHandler? CanExecuteChanged;

			private readonly IAction action;
			private readonly ICommand command;

			public CommandCodes Code { get; }

			public string Label => action.Label;

			public bool IsToggle => action is IToggleAction;

			public bool IsOn
			{
				get => action is IToggleAction toggleAction && toggleAction.IsOn;
				set
				{
					if (action is IToggleAction toggleAction && toggleAction.IsOn != value)
						command.Execute(null);
				}
			}

			public bool IsExecutable => action.IsExecutable;

			public ActionCommand(CommandCodes code, IAction action)
			{
				Code = code;
				this.action = action;

				command = new AsyncRelayCommand(ExecuteAsync, () => action.IsExecutable);

				if (action is INotifyPropertyChanging notifyPropertyChanging)
					notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
				if (action is INotifyPropertyChanged notifyPropertyChanged)
					notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
			}

			public bool CanExecute(object? parameter) => command.CanExecute(parameter);
			public void Execute(object? parameter) => command.Execute(parameter);

			public Task ExecuteAsync()
			{
				if (IsExecutable)
					return action.ExecuteAsync();
				return Task.CompletedTask;
			}

			public async void ExecuteTapped(object _, TappedRoutedEventArgs e) => await action.ExecuteAsync();

			private void Action_PropertyChanging(object? _, PropertyChangingEventArgs e)
			{
				switch (e.PropertyName)
				{
					case nameof(IAction.Label):
						OnPropertyChanging(nameof(Label));
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
}
