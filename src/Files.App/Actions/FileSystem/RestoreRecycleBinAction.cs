using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class RestoreRecycleBinAction : BaseUIAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override string Label { get; } = "Restore".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRestoreItem");

		public override bool IsExecutable =>
			context.PageType is ContentPageTypes.RecycleBin &&
			context.SelectedItems.Any() &&
			UIHelpers.CanShowDialog;

		public RestoreRecycleBinAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public override async Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				await RecycleBinHelpers.RestoreSelectionRecycleBin(context.ShellPage);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItems):
					if (context.PageType is ContentPageTypes.RecycleBin)
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
