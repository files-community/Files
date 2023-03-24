using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.UserControls.MultitaskingControl;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class ReopenClosedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label => "ReopenClosedTab".GetLocalizedResource();

		public string Description => "TODO: Need to be described";

		public HotKey HotKey { get; } = new(VirtualKey.T, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public bool IsExecutable =>
			context.Control is not null &&
			!BaseMultitaskingControl.IsRestoringClosedTab &&
			BaseMultitaskingControl.RecentlyClosedTabs.Any();

		public ReopenClosedTabAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
			BaseMultitaskingControl.IsRestoringTabChanged += BaseMultitaskingControl_IsRestoringTabChanged;
			BaseMultitaskingControl.RecentlyClosedTabs.CollectionChanged += RecentlyClosedTabs_CollectionChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.ReopenClosedTab();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IMultitaskingContext.Control))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void BaseMultitaskingControl_IsRestoringTabChanged(object? _, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}

		private void RecentlyClosedTabs_CollectionChanged(object? _, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
