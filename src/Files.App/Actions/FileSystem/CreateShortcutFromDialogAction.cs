using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateShortcutFromDialogAction : BaseUIAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override string Label { get; } = "Shortcut".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconShortcut");

		public override bool IsExecutable => context.ShellPage is not null && UIHelpers.CanShowDialog;

		public CreateShortcutFromDialogAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public override async Task ExecuteAsync()
		{
			await UIFilesystemHelpers.CreateShortcutFromDialogAsync(context.ShellPage);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
