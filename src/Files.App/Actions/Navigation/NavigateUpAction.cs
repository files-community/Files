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
	internal class NavigateUpAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Up".GetLocalizedResource();

		public string Description { get; } = "NavigateUp".GetLocalizedResource();

		public HotKey HotKey { get; } = new(VirtualKey.Up, VirtualKeyModifiers.Menu);

		public RichGlyph Glyph { get; } = new("\uE74A");

		public bool CanExecute => context.CanNavigateToParent;

		public NavigateUpAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.Up_Click();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanNavigateToParent):
					NotifyCanExecuteChanged();
					break;
			}
		}
	}
}
