using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal abstract class BaseSetAsAction : ObservableObject, IAction
	{
		protected readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public virtual bool IsExecutable => context.ShellPage is not null &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder &&
			(context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false);

		public BaseSetAsAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IContentPageContext.SelectedItem):
				case nameof(IContentPageContext.SelectedItems):
					if (context.ShellPage is not null && context.ShellPage.SlimContentPage is not null)
					{
						var viewModel = context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
						var extensions = context.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

						viewModel.CheckAllFileExtensions(extensions);
					}

					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		public abstract Task ExecuteAsync();
	}
}
