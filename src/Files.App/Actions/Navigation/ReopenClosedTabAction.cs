using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.UserControls.MultitaskingControl;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class ReopenClosedTabAction : XamlUICommand
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "ReopenClosedTab".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described";

		public HotKey HotKey { get; } = new(VirtualKey.T, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public bool CanExecute =>
			context.Control is not null &&
			!BaseMultitaskingControl.IsRestoringClosedTab &&
			BaseMultitaskingControl.RecentlyClosedTabs.Count > 0;

		public ReopenClosedTabAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
			BaseMultitaskingControl.StaticPropertyChanged += BaseMultitaskingControl_StaticPropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.ReopenClosedTab();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IMultitaskingContext.Control))
				NotifyCanExecuteChanged();
		}

		private void BaseMultitaskingControl_StaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			NotifyCanExecuteChanged();
		}
	}
}
