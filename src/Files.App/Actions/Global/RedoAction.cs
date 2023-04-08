using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class RedoAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Redo".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(Keys.Y, KeyModifiers.Ctrl);

		public bool IsExecutable => context.ShellPage is not null &&
			context.PageType is not ContentPageTypes.SearchResults;

		public RedoAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return context.ShellPage!.StorageHistoryHelpers.TryRedo();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
