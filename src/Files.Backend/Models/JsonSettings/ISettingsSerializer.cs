namespace Files.Backend.Models.JsonSettings
{
    public interface ISettingsSerializer
    {
        bool WriteToFile(string json);

        string ReadFromFile();
    }
}
