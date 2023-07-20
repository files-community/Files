﻿// Copyright (c) 2023 Files Community
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
	internal class PasteItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

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
			App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return;

			string path = context.ShellPage.FilesystemViewModel.WorkingDirectory;
			await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
		}

		public bool GetIsExecutable()
		{
			return
				App.AppModel.IsPasteEnabled &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.SearchResults;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
