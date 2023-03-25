using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Views;
using Files.App.Views.LayoutModes;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class FindAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Share".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.F, VirtualKeyModifiers.Control);

		public HotKey SecondHotKey { get; } = new(VirtualKey.F3);

		public RichGlyph Glyph { get; } = new();

		public bool IsExecutable =>
			context.ShellPage is not null &&
			IsPageTypeValid();

		public FindAction()
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
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageLayoutType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private bool IsPageTypeValid()
		{
			return
			(context.ShellPage is ModernShellPage &&
			(
				context.PageLayoutType == typeof(DetailsLayoutBrowser) ||
				context.PageLayoutType == typeof(GridViewBrowser) ||
				context.PageLayoutType == typeof(WidgetsPage)
			)) ||
			(context.ShellPage is ColumnShellPage &&
			(
				context.PageLayoutType == typeof(DetailsLayoutBrowser) ||
				context.PageLayoutType == typeof(GridViewBrowser) ||
				context.PageLayoutType == typeof(ColumnViewBase) ||
				context.PageLayoutType == typeof(ColumnViewBrowser
			)));
		}
	}
}
