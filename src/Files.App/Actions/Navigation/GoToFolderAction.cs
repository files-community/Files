// Copyright (c) Files Community
// Licensed under the MIT License.

using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Data.Commands;
using Files.App.Data.Contexts;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class GoToFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		public string Label
		{
			get
			{
				var localized = Strings.Command_GoToFolder_Label.GetLocalizedResource();
				return string.IsNullOrEmpty(localized) ? "Go to folder..." : localized;
			}
		}

		public string Description
		{
			get
			{
				var localized = Strings.Command_GoToFolder_Description.GetLocalizedResource();
				return string.IsNullOrEmpty(localized) ? "Quick jump to frequently visited folders" : localized;
			}
		}

		public ActionCategory Category => ActionCategory.Navigation;

		public RichGlyph Glyph => new("\uED43", themedIconStyle: "App.ThemedIcons.Folder");

		// Atajo por defecto: Ctrl + G
		public HotKey HotKey => new(Keys.G, KeyModifiers.Ctrl);

		public bool IsExecutable => _context.ShellPage is not null;

		public GoToFolderAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (_context.ShellPage?.ToolbarViewModel is { } toolbar)
			{
				_ = toolbar.SwitchToGoToMode();
			}

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
