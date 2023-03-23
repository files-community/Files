using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class SetAsLockscreenBackgroundAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "SetAsLockscreen".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uEE3F");

		public bool IsExecutable => context.ShellPage is not null &&
			context.SelectedItem is not null &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder &&
			(context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public SetAsLockscreenBackgroundAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.SelectedItem is not null)
				WallpaperHelpers.SetAsBackground(WallpaperType.LockScreen, context.SelectedItem.ItemPath);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IContentPageContext.SelectedItem):
					if (context.ShellPage is not null && context.ShellPage.SlimContentPage is not null)
					{
						var viewModel = context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
						var extensions = context.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

						viewModel.CheckAllFileExtensions(extensions);
					}

					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
