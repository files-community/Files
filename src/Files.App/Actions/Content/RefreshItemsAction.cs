using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class RefreshItemsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Refresh".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE72C");

		public HotKey HotKey { get; } = new(VirtualKey.R, VirtualKeyModifiers.Control);

		public bool IsExecutable => context.ShellPage?.ToolbarViewModel?.CanRefresh ?? false;

		public RefreshItemsAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			context.ShellPage?.Refresh_Click();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage.ToolbarViewModel.CanRefresh))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
