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
using Windows.Storage;

namespace Files.App.Actions
{
	internal class PinItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IQuickAccessService service = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string Label { get; } = "PinToFavorites".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconPinToFavorites");

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public PinItemAction()
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
				await service.PinToSidebar(items);
			}
			else if (context.Folder is not null)
			{
				await service.PinToSidebar(context.Folder.ItemPath);
			}
		}

		private bool GetIsExecutable()
		{
			string[] favorites = App.QuickAccessManager.Model.FavoriteItems.ToArray();

			return context.HasSelection
				? context.SelectedItems.All(IsPinnable)
				: context.Folder is not null && IsPinnable(context.Folder);

			bool IsPinnable(ListedItem item)
			{
				return item.PrimaryItemAttribute is StorageItemTypes.Folder
					&& !favorites.Contains(item.ItemPath);
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
