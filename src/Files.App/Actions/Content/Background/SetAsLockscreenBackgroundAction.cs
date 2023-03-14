using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions.Content.Background
{
	internal class SetAsLockscreenBackgroundAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "SetAsLockscreen".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uEE3F");

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public SetAsLockscreenBackgroundAction()
		{
			isExecutable = GetIsExecutable();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.SelectedItem is not null)
				WallpaperHelpers.SetAsBackground(WallpaperType.LockScreen, context.SelectedItem.ItemPath);
			return Task.CompletedTask;
		}

		private bool GetIsExecutable() => context.ShellPage is not null && context.SelectedItem is not null
			&& context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder
			&& (context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItem):
					SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
					break;
			}
		}
	}
}
