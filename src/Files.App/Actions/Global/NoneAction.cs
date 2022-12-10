using Files.App.Commands;
using System.Threading.Tasks;

namespace Files.App.Actions
{
    internal class NoneAction : IAction
	{
		public CommandCodes Code => CommandCodes.None;
		public string Label => string.Empty;

		public Task ExecuteAsync() => Task.CompletedTask;
	}
}
