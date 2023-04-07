using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class NavigateBackAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Back".GetLocalizedResource();

		public string Description { get; } = "NavigateBack".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Left, KeyModifiers.Menu);
		public HotKey SecondHotKey { get; } = new(Keys.Back);
		public HotKey ThirdHotKey { get; } = new(Keys.Mouse4);
		public HotKey MediaHotKey { get; } = new(Keys.GoBack, false);

		public RichGlyph Glyph { get; } = new("\uE72B");

		public bool IsExecutable => context.CanGoBack;

		public NavigateBackAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.Back_Click();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanGoBack):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
