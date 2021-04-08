using Microsoft.Toolkit.Uwp;
using System.Globalization;

namespace Files.DataModels
{
    public class DefaultLanguageModel
    {
        public DefaultLanguageModel(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var info = new CultureInfo(id);
                ID = info.Name;
                Name = info.NativeName;
            }
            else
            {
                ID = string.Empty;
                var systemDefaultLanguageOptionStr = "SettingsPreferencesSystemDefaultLanguageOption".GetLocalized();
                Name = string.IsNullOrEmpty(systemDefaultLanguageOptionStr) ? "System Default" : systemDefaultLanguageOptionStr;
            }
        }

        public string ID { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}