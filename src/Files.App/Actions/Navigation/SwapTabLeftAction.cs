using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class SwapTabLeftAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "SwapTabLeft".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public bool IsExecutable => multitaskingContext.TabCount > 1;

		public HotKey HotKey { get; } = new(VirtualKey.Tab, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public SwapTabLeftAction()
		{
			multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (App.AppModel.TabStripSelectedIndex is 0)
				App.AppModel.TabStripSelectedIndex = multitaskingContext.TabCount - 1;
			else
				App.AppModel.TabStripSelectedIndex--;
			return Task.CompletedTask;
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
