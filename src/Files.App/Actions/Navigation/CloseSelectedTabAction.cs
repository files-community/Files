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
	internal class CloseSelectedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "CloseTab".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.W, VirtualKeyModifiers.Control);

		public HotKey SecondHotKey { get; } = new(VirtualKey.F4, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new();

		public bool IsExecutable =>
			context.Control is not null && 
			context.TabCount > 0 && 
			context.CurrentTabItem is not null;

		public CloseSelectedTabAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.CloseTab(context.CurrentTabItem);
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.CurrentTabItem):
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
