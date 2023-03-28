using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class RenameAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Rename".GetLocalizedResource();
		
		public string Description { get; } = "TODO";

		public HotKey HotKey { get; } = new(VirtualKey.F2);

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconRename");

		public bool CanExecute => 
			context.ShellPage is not null && 
			IsPageTypeValid() &&
			context.ShellPage.SlimContentPage is not null && 
			IsSelectionValid();

		public RenameAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel.StartRenameItem();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					NotifyCanExecuteChanged();
					break;
			}
		}

		private bool IsSelectionValid()
		{
			return context.HasSelection && context.SelectedItems.Count == 1;
		}

		private bool IsPageTypeValid()
		{
			return context.PageType is
				not ContentPageTypes.None and
				not ContentPageTypes.Home and
				not ContentPageTypes.RecycleBin and
				not ContentPageTypes.ZipFolder;
		}
	}
}
