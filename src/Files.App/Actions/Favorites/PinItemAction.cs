using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.ServicesImplementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Actions.Favorites
{
	internal class PinItemAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly IQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label { get; } = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconPinToFavorites");

		public async Task ExecuteAsync()
		{
			 await quickAccessService.PinToSidebar(context.SelectedItems.Any() ? context.SelectedItems.Select(x => x.ItemPath).ToArray() : new[] { context.Folder.ItemPath });
		}
	}
}
