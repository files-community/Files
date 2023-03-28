using Microsoft.UI.Xaml.Input;
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
	internal class RefreshItemsAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Refresh".GetLocalizedResource();
		public string Description { get; } = "TODO";

		public RichGlyph Glyph { get; } = new("\uE72C");

		public HotKey HotKey { get; } = new(VirtualKey.R, VirtualKeyModifiers.Control);

        public HotKey SecondHotKey { get; } = new(VirtualKey.F5);
        
		public bool CanExecute => context.CanRefresh;

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
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanRefresh):
					NotifyCanExecuteChanged();
					break;
			}
		}
	}
}
