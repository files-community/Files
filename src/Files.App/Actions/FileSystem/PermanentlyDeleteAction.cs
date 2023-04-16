using Files.App.Commands;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class PermanentlyDeleteAction : BaseDeleteAction, IAction
	{
		public string Label { get; } = "PermanentlyDelete".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(Keys.Delete, KeyModifiers.Shift);

		public Task ExecuteAsync()
		{
			return DeleteItems(true);
		}
	}
}
