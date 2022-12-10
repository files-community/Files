using Files.App.Commands;
using Files.App.DataModels;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	public interface IAction
	{
		CommandCodes Code { get; }
		string Label { get; }

		IGlyph Glyph => Commands.Glyph.None;
		HotKey HotKey => HotKey.None;

		bool IsOn => false;
		bool IsExecutable => true;

		Task ExecuteAsync();
	}
}
