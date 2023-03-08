using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Shared.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions.Content.Tags
{
	internal class OpenAllTaggedAction : IAction
	{
		private readonly ITagsContext tagsContext = Ioc.Default.GetRequiredService<ITagsContext>();
		
		private readonly IContentPageContext pageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "OpenAllItems".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE8E5");

		public async Task ExecuteAsync()
		{
			if (pageContext.ShellPage is null)
				return;

			var files = tagsContext.TaggedItems.Where(taggedItem => !taggedItem.IsFolder);
			var folders = tagsContext.TaggedItems.Where(taggedItem => taggedItem.IsFolder);

			await Task.WhenAll(files.Select(file => NavigationHelpers.OpenPath(file.Path, pageContext.ShellPage)));
			folders.ForEach(async folder => await NavigationHelpers.OpenPathInNewTab(folder.Path));
		}
	}
}
