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
	internal class ShowPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Properties".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconProperties");

		public bool IsExecutable =>
			context.ShellPage.InstanceViewModel.IsPageTypeNotHome &&
			context.HasSelection;

		public HotKey HotKey { get; } = new(VirtualKey.P, VirtualKeyModifiers.Control);

		public ShowPropertiesAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			FilePropertiesHelpers.ShowProperties(context.ShellPage);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage.InstanceViewModel.IsPageTypeNotHome):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
