using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Shell;
using Files.Backend.Helpers;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class RunAsAnotherUserAction : XamlUICommand
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		public bool CanExecute => context.SelectedItem is not null &&
			FileExtensionHelpers.IsExecutableFile(context.SelectedItem.FileExtension);
		public string Label => "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";
		public RichGlyph Glyph => new("\uE7EE");

		public RunAsAnotherUserAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("runasuser", context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					NotifyCanExecuteChanged();
					break;
			}
		}
	}
}
