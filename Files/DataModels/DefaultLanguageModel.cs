using System.Globalization;

namespace Files.DataModels
{
    public class DefaultLanguageModel
    {
        public string ID;
        public string Name;

        public DefaultLanguageModel(string id)
        {
            var info = new CultureInfo(id);
            ID = info.Name;
            Name = info.DisplayName;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}