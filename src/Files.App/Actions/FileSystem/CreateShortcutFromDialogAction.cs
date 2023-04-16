using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateShortcutFromDialogAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Shortcut".GetLocalizedResource();

		public string Description => "CreateShortcutFromDialogDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconShortcut");

		public override bool IsExecutable => context.CanCreateItem && UIHelpers.CanShowDialog;

		public CreateShortcutFromDialogAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await UIFilesystemHelpers.CreateShortcutFromDialogAsync(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanCreateItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
