using System.Threading.Tasks;

namespace Files.Core
{
	public interface ILogWriter
	{
		Task InitializeAsync(string name);
		Task WriteLineToLogAsync(string text);
		void WriteLineToLog(string text);
	}
}
