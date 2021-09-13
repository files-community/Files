using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Models.JsonSettings
{
    public interface ISettingsSharingContext
    {
        string FilePath { get; }

        IJsonSettingsDatabase JsonSettingsDatabase { get; }

        ISettingsSharingContext GetContext();
    }
}
