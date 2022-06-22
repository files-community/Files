namespace Files.Shared.Services
{
    public interface IFullTrustResponse
    {
        bool IsSuccess { get; }

        bool ContainsKey(string key);

        T Get<T>(string key);
        T Get<T>(string key, T defaultValue);
    }
}
