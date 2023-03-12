using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal abstract class GroupByAction : ObservableObject, IToggleAction
	{
		protected IContentPageContext ContentContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IDisplayPageContext DisplayContext { get; } = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		protected abstract GroupOption GroupOption { get; }

		public abstract string Label { get; }

		private bool isOn;
		public bool IsOn => isOn;

		private bool isExecutable = false;
		public bool IsExecutable => isExecutable;

		public GroupByAction()
		{
			isOn = DisplayContext.GroupOption == GroupOption;
			isExecutable = GetIsExecutable(ContentContext.PageType);

			ContentContext.PropertyChanged += ContentContext_PropertyChanged;
			DisplayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			DisplayContext.GroupOption = GroupOption;
			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType) => true;

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				SetProperty(ref isExecutable, GetIsExecutable(ContentContext.PageType), nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.GroupOption))
				SetProperty(ref isOn, DisplayContext.GroupOption == GroupOption, nameof(IsOn));
		}
	}
}
