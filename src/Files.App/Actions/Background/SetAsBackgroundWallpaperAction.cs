using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions.Background
{
	internal class SetAsWallpaperBackgroundAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "SetAsBackground".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph("\uE91B");

		public bool IsExecutable => IsContextPageTypeAdaptedToCommand() && context.SelectedItems.Count > 1;

		public SetAsWallpaperBackgroundAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
			{
				var imagePaths = context.SelectedItems.Select(item => item.ItemPath).ToArray();
				WallpaperHelpers.SetSlideshow(imagePaths);
			}
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin 
				and not ContentPageTypes.ZipFolder 
				and not ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
