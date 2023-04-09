using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Views;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class RefreshItemsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Refresh".GetLocalizedResource();
		public string Description { get; } = "TODO";

		public RichGlyph Glyph { get; } = new("\uE72C");

		public HotKey HotKey { get; } = new(VirtualKey.R, VirtualKeyModifiers.Control);

        public HotKey SecondHotKey { get; } = new(VirtualKey.F5);
        
		public bool IsExecutable => context.CanRefresh;

		public RefreshItemsAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is BaseShellPage bsp)
			{
				bsp.ToolbarViewModel.RefreshClickCommand.Execute(null);
			}
			else
			{
				context.ShellPage?.Refresh_Click();
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanRefresh):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
