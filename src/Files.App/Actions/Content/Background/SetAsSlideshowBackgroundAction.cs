using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions.Content.Background
{
	internal class SetAsSlideshowBackgroundAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "SetAsSlideshow".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE91B");

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public SetAsSlideshowBackgroundAction()
		{
			isExecutable = GetIsExecutable();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var paths = context.SelectedItems.Select(item => item.ItemPath).ToArray();
			WallpaperHelpers.SetSlideshow(paths);

			return Task.CompletedTask;
		}

		private bool GetIsExecutable() => context.ShellPage is not null && context.SelectedItems.Count > 1
			&& context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder
			&& (context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItems):
					SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
					break;
			}
		}
	}
}
