using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class SearchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Search".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(Keys.F, KeyModifiers.Ctrl);
		public HotKey SecondHotKey { get; } = new(Keys.F3);

		public RichGlyph Glyph { get; } = new();

		public bool IsExecutable => !context.IsSearchBoxVisible;

		public SearchAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.ToolbarViewModel.SwitchSearchBoxVisibility();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsSearchBoxVisible):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
