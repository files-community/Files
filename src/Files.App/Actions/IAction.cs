using System.Threading.Tasks;

namespace Files.App.Actions
{
	public interface IAction
	{
		string Label { get; }

		bool IsExecutable => true;

		Task ExecuteAsync();
	}
}
