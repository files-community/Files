using Files.App.Commands;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	public interface IAction
	{
		string Label { get; }

		RichGlyph Glyph => RichGlyph.None;

		HotKey HotKey => HotKey.None;

		bool IsExecutable => true;

		Task ExecuteAsync();
	}
}
