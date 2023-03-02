using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class DeleteItemAction : ObservableObject, IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Delete".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public HotKey HotKey = new(VirtualKey.Delete);

		public bool IsExecutable => context.ShellPage is not null && context.SelectedItems is not null
			&& context.SelectedItems.Any() && context.PageType is not ContentPageTypes.Home;

		public DeleteItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.DeleteItem(context.ShellPage);
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
