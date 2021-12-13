using System.Threading.Tasks;

namespace Files.Common
{
    public interface ILogWriter
    {
        Task InitializeAsync(string name);
        Task WriteLineToLogAsync(string text);
        void WriteLineToLog(string text);
    }
}
