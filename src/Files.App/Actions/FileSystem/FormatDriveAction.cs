using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Shell;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class FormatDriveAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "FormatDriveText".GetLocalizedResource();

		public string Description { get; } = "FormatDriveDescription".GetLocalizedResource();
		public bool IsExecutable => context.HasItem && (App.DrivesManager.Drives.FirstOrDefault(x => string.Equals(x.Path, context.Folder?.ItemPath))?.MenuOptions.ShowFormatDrive ?? false);

		public FormatDriveAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			Win32API.OpenFormatDriveDialog(context.Folder?.ItemPath ?? string.Empty);
			return Task.CompletedTask;
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
