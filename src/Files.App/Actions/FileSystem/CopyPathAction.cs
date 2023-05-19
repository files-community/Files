// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal class CopyPathAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "CopyAsPath".GetLocalizedResource();

		public string Description => "CopyPathDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconCopyPath");

		public HotKey HotKey { get; } = new(Keys.C, KeyModifiers.CtrlShift);

		public Task ExecuteAsync()
		{
			if (context.ShellPage?.SlimContentPage is not null)
			{
				var path = context.ShellPage.SlimContentPage.SelectedItem is not null
					? context.ShellPage.SlimContentPage.SelectedItem.ItemPath
					: context.ShellPage.FilesystemViewModel.WorkingDirectory;

				if (FtpHelpers.IsFtpPath(path))
					path = path.Replace("\\", "/", StringComparison.Ordinal);

				DataPackage data = new();
				data.SetText(path);

				Clipboard.SetContent(data);
				Clipboard.Flush();
			}

			return Task.CompletedTask;
		}
	}
}
