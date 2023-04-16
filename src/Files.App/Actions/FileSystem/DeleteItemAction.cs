using Files.App.Commands;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DeleteItemAction : BaseDeleteAction, IAction
	{
		public string Label { get; } = "Delete".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public HotKey HotKey { get; } = new(Keys.Delete);

		public HotKey SecondHotKey { get; } = new(Keys.D, KeyModifiers.Ctrl);

		public Task ExecuteAsync()
		{
			return DeleteItems(false);
		}
	}
}
