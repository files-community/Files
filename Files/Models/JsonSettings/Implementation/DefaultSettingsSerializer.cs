using Files.Helpers;

namespace Files.Models.JsonSettings.Implementation
{
    public class DefaultSettingsSerializer : ISettingsSerializer
    {
        private readonly string _filePath;

        public DefaultSettingsSerializer(string filePath)
        {
            this._filePath = filePath;
        }

        public string ReadFromFile()
        {
            return NativeFileOperationsHelper.ReadStringFromFile(_filePath);
        }

        public bool WriteToFile(string json)
        {
            return NativeFileOperationsHelper.WriteStringToFile(_filePath, json);
        }
    }
}
