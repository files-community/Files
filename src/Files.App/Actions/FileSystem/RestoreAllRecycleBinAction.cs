using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class RestoreAllRecycleBinAction : BaseUIAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override string Label { get; } = "RestoreAllItems".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRestoreItem");

		public override bool IsExecutable =>
			context.ShellPage is not null &&
			UIHelpers.CanShowDialog &&
			((context.PageType is ContentPageTypes.RecycleBin && context.HasItem) || 
			RecycleBinHelpers.RecycleBinHasItems());

		public RestoreAllRecycleBinAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public override async Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				await RecycleBinHelpers.RestoreRecycleBin(context.ShellPage);
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
