// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Data.Models;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal sealed class PasteItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private IWindowContext WindowContext { get; } = Ioc.Default.GetRequiredService<IWindowContext>();

		public string Label
			=> "Paste".GetLocalizedResource();

		public string Description
			=> "PasteItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPaste");

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> GetIsExecutable();

		public PasteItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			WindowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			string path = context.ShellPage.FilesystemViewModel.WorkingDirectory;
			await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
		}

		public bool GetIsExecutable()
		{
			return
				App.WindowContext.IsPasteEnabled &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.SearchResults;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IWindowContext.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
