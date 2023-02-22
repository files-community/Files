using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class EmptyRecycleBinAction : IAction
	{
		public string Label { get; } = "EmptyRecycleBin".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph("\uEF88", fontFamily: "RecycleBinIcons");

		public async Task ExecuteAsync() => await RecycleBinHelpers.EmptyRecycleBin();
	}
}
