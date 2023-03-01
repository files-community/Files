using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class CopyItemAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Copy".GetLocalizedResource();

		public HotKey HotKey = new(VirtualKey.C, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconCopy");

		public async Task ExecuteAsync()
		{
			await UIFilesystemHelpers.CopyItem(context.ShellPage);
		}
	}
}
