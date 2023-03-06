using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Shell;
using Files.Backend.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class RunAsAdminAction : ObservableObject, IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		public bool IsExecutable => context.SelectedItem is not null &&
			FileExtensionHelpers.IsExecutableFile(context.SelectedItem.FileExtension);
		public string Label => "RunAsAdministrator".GetLocalizedResource();
		public RichGlyph Glyph => new("\uE7EF");

		public RunAsAdminAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("runas", context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
