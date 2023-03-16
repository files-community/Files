using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.ServicesImplementation;
using Files.App.UserControls.Widgets;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class UnpinItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IQuickAccessService service = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label { get; } = "UnpinFromFavorites".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconUnpinFromFavorites");

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public UnpinItemAction()
		{
			isExecutable = GetIsExecutable();

			context.PropertyChanged += Context_PropertyChanged;
			App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.HasSelection)
			{
				var items = context.SelectedItems.Select(x => x.ItemPath).ToArray();
				await service.UnpinFromSidebar(items);
			}
			else if (context.Folder is not null)
			{
				await service.UnpinFromSidebar(context.Folder.ItemPath);
			}
		}

		private bool GetIsExecutable()
		{
			string[] favorites = App.QuickAccessManager.Model.FavoriteItems.ToArray();

			return context.HasSelection
				? context.SelectedItems.All(IsPinned)
				: context.Folder is not null && IsPinned(context.Folder);

			bool IsPinned(ListedItem item)
			{
				return favorites.Contains(item.ItemPath);
			}
		}
		private void UpdateIsExecutable()
		{
			SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItems):
					UpdateIsExecutable();
					break;
			}
		}

		private void QuickAccessManager_DataChanged(object? sender, ModifyQuickAccessEventArgs e)
		{
			UpdateIsExecutable();
		}
	}
}
