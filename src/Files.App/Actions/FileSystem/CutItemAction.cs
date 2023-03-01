using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class CutItemAction : ObservableObject, IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "BaseLayoutItemContextFlyoutCut/Text".GetLocalizedResource();

		public HotKey HotKey = new(VirtualKey.X, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconCut");

		public bool IsExecutable => context.ShellPage is not null && context.SelectedItems is not null
			&& context.SelectedItems.Any() && context.PageType is not ContentPageTypes.Home;

		public CutItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			UIFilesystemHelpers.CutItem(context.ShellPage);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}