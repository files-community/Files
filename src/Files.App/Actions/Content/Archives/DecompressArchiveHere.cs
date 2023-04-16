using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DecompressArchiveHere : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "ExtractHere".GetLocalizedResource();

		public string Description => "DecompressArchiveHereDescription".GetLocalizedResource();

		public override bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanDecompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public DecompressArchiveHere()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ArchiveHelpers.DecompressArchiveHere(context.ShellPage);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
