using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class DeleteItemAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Delete".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public HotKey HotKey = new(VirtualKey.Delete);

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.DeleteItem(context.ShellPage);
		}
	}
}
