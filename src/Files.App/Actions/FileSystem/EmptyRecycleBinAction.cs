using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class EmptyRecycleBinAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "EmptyRecycleBin".GetLocalizedResource();

		public string Description => "EmptyRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			((context.PageType is ContentPageTypes.RecycleBin && context.HasItem) ||
			RecycleBinHelpers.RecycleBinHasItems());

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
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
