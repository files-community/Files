using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.Commands
{
	internal class HotKeyAccelerator : IHotKeyAccelerator
	{
		private readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();

		private IList<KeyboardAccelerator> accelerators = Array.Empty<KeyboardAccelerator>();

		public void Initialize(IList<KeyboardAccelerator> accelerators)
		{
			accelerators.OfType<CommandAccelerator>().ForEach(a => a.Dispose());
			if (this.accelerators.Any())
				this.accelerators.Clear();

			var hotKeys = Ioc.Default.GetService<IHotKeyManager>();
			if (hotKeys is null)
				return;

			this.accelerators = accelerators;

			hotKeys.HotKeyChanged += HotKeyManager_HotKeyChanged;

			foreach (IRichCommand command in commands)
				if (!command.UserHotKey.IsNone)
					this.accelerators.Add(new CommandAccelerator(command));
		}

		private void HotKeyManager_HotKeyChanged(IHotKeyManager manager, HotKeyChangedEventArgs e)
		{
			if (e.OldCommandCode is not CommandCodes.None && !e.OldHotKey.IsNone)
			{
				var oldAccelerator = accelerators
					.OfType<CommandAccelerator>()
					.FirstOrDefault(a => a.Command.Code == e.OldCommandCode);
				if (oldAccelerator is not null)
				{
					oldAccelerator.Dispose();
					accelerators.Remove(oldAccelerator);
				}
			}
			if (e.NewCommandCode is not CommandCodes.None && !e.NewHotKey.IsNone)
			{
				var newCommand = commands[e.NewCommandCode];
				accelerators.Add(new CommandAccelerator(newCommand));
			}
		}

		private class CommandAccelerator : KeyboardAccelerator, IDisposable
		{
			public IRichCommand Command { get; }

			public CommandAccelerator(IRichCommand command)
			{
				Command = command;

				Key = Command.UserHotKey.Key;
				Modifiers = Command.UserHotKey.Modifiers;
				Invoked += CommandAccelerator_Invoked;
			}

			public void Dispose() => Invoked -= CommandAccelerator_Invoked;

			private async void CommandAccelerator_Invoked(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs e)
			{
				e.Handled = true;
				await Command.ExecuteAsync();
			}
		}
	}
}
