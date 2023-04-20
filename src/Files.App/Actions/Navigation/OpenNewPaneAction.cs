using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class OpenNewPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "NavigationToolbarNewPane/Label".GetLocalizedResource();

		public string Description { get; } = "OpenNewPaneDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.OemPlus, KeyModifiers.MenuShift);

		public HotKey SecondHotKey { get; } = new(Keys.Add, KeyModifiers.MenuShift);

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconRightPane");

		public bool IsExecutable => context.IsMultiPaneEnabled && !context.IsMultiPaneActive;

		public OpenNewPaneAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.PaneHolder.OpenPathInNewPane("Home");
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsMultiPaneEnabled):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
