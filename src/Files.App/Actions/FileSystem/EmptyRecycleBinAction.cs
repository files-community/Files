using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class EmptyRecycleBinAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "EmptyRecycleBin".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public bool IsExecutable
		{
			get
			{
				if (context.PageType is ContentPageTypes.RecycleBin)
					return context.HasItem;
				return RecycleBinHelpers.RecycleBinHasItems();
			}
		}

		public EmptyRecycleBinAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.EmptyRecycleBin();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasItem):
					if (context.PageType is ContentPageTypes.RecycleBin)
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
