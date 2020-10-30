using Microsoft.Toolkit.Uwp.Extensions;
using System.Globalization;

namespace Files.DataModels
{
    public class DefaultLanguageModel
    {
        public string ID;
        public string Name;

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

        public override string ToString()
        {
            return Name;
        }
    }
}