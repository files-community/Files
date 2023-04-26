using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class PreviousTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "PreviousTab".GetLocalizedResource();

		public string Description { get; } = "PreviousTabDescription".GetLocalizedResource();

		public bool IsExecutable => multitaskingContext.TabCount > 1;

		public HotKey HotKey { get; } = new(Keys.Tab, KeyModifiers.CtrlShift);

		public PreviousTabAction()
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
