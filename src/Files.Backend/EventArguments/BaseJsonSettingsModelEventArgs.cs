using System;

namespace Files.Backend.EventArguments
{
    public sealed class SettingChangedEventArgs : EventArgs
    {
        public readonly string settingName;

        public readonly object newValue;

        public SettingChangedEventArgs(string settingName, object newValue)
        {
            this.settingName = settingName;
            this.newValue = newValue;
        }
    }
}
