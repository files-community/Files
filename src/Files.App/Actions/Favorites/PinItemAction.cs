﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.ServicesImplementation;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions.Favorites
{
	internal class PinItemAction : ObservableObject, IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly IQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label { get; } = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconPinToFavorites");

		public bool IsExecutable
		{
			get
			{
				if ((context.SelectedItems.Any() && context.SelectedItems.All(x => !x.IsPinned))
					|| (context.Folder is not null && !context.Folder.IsPinned))
					return true;

				return false;
			}
		}

		public PinItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			 await quickAccessService.PinToSidebar(context.SelectedItems.Any() ? context.SelectedItems.Select(x => x.ItemPath).ToArray() : new[] { context.Folder.ItemPath });
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
