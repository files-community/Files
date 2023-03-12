using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal abstract class SortByAction : ObservableObject, IToggleAction
	{
		private IContentPageContext contentContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IDisplayPageContext displayContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		protected abstract SortOption SortOption { get; }

		public abstract string Label { get; }

		private bool isOn;
		public bool IsOn => isOn;

		private bool isExecutable = false;
		public bool IsExecutable => isExecutable;

		public SortByAction()
		{
			isOn = displayContext.SortOption == SortOption;
			isExecutable = GetIsExecutable(contentContext.PageType);

			contentContext.PropertyChanged += ContentContext_PropertyChanged;
			displayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			displayContext.SortOption = SortOption;
			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType) => true;

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				SetProperty(ref isExecutable, GetIsExecutable(contentContext.PageType), nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortOption))
				SetProperty(ref isOn, displayContext.SortOption == SortOption, nameof(IsOn));
		}
	}
}
