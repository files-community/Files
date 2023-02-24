using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Helpers;

using System.Threading.Tasks;

namespace Files.App.Actions.FileSystem
{
	internal class RestoreRecycleBinAction : IAction
	{
		public string Label { get; } = "SideBarRestoreRecycleBin/Text".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph("\xE777");

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.RestoreRecycleBin();
		}
	}
}
